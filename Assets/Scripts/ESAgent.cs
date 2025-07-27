using UnityEngine;

[RequireComponent(typeof(AgentController))]
public class ESAgent : MonoBehaviour, IAgentView
{
    public Transform Tf => transform;
    public Rigidbody Rb { get; private set; }
    public float CumulativeReward { get; private set; }
    public bool Done { get; private set; }

    IObservation[] obsProviders;
    IReward[] rewProviders;
    IPolicy policy;

    float[] buffer;
    float[] action;

    const float MaxEpisodeTime = 3f;
    float timer;

    bool despawned;

    AgentController agentController;
    Collider[] colliders;
    Renderer[] renderers;

    const int ActiveLayer = 6;
    const int PooledLayer = 7;

    void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        agentController = GetComponent<AgentController>();
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void Init(IPolicy _policy, float[] theta, IObservation[] obs, IReward[] rews)
    {
        policy = _policy;
        policy.SetParams(theta);
        obsProviders = obs;
        rewProviders = rews;

        buffer = new float[policy.InputDim];
        action = new float[policy.OutputDim];

        foreach (var r in rewProviders) r.Reset();
        CumulativeReward = 0;
        Done = false;
        timer = 0f;
    }

    public void Step(float dt)
    {
        if (Done) return;

        // 1) Build observation buffer
        int offset = 0;
        foreach (var o in obsProviders)
            offset = o.Write(buffer, offset);

        // 2) Policy -> action
        policy.Act(buffer, action);

        // 3) Feed to locomotion / animation
        Vector3 dir = new Vector3(action[0], 0, action[1]).normalized;
        agentController.DesiredDirection = dir;

        // 4) Rewards & termination
        foreach (var r in rewProviders)
        {
            r.Step(out var rwd);
            CumulativeReward += rwd;
            if (r.Done)
            {
                Done = true;
            }
        }

        timer += dt;
        if (timer > MaxEpisodeTime)
            Done = true;

        if(Done) Despawn();
    }

    public void Despawn()
    {
        if (despawned) return;
        despawned = true;

        agentController.enabled = false;
        gameObject.layer = PooledLayer;

        Rb.velocity = Rb.angularVelocity = Tf.position = Vector3.zero;
    }

    public void Respawn()
    {
        despawned = false;

        agentController.enabled = true;
        gameObject.layer = ActiveLayer;
    }
}