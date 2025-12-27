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
    Collider coll;
    Renderer rend;

    void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        agentController = GetComponent<AgentController>();
        coll = GetComponent<Collider>();
        rend = GetComponent<Renderer>();
    }

    // Called one when Start Training is clicked
    public void ConfigureAgent(IPolicy _policy, IObservation[] _obs, IReward[] _rews)
    {
        policy = _policy;
        obsProviders = _obs;
        rewProviders = _rews;

        if (buffer == null || buffer.Length != policy.InputDim)
            buffer = new float[policy.InputDim];

        if (action == null || action.Length != policy.OutputDim)
            action = new float[policy.OutputDim];

        foreach (var o in obsProviders) o.ag = this;
        foreach (var r in rewProviders) r.ag = this;
    }

    // Called every generation
    public void PrepareForRun(float[] theta)
    {
        // Inject new weights into existing brain
        policy.SetParams(theta);

        // Reset sensors/rewards
        foreach (var r in rewProviders) r.Reset();

        // If observations have state, reset them here too
        // foreach (var o in obsProviders) o.Reset();

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

        // 3) Feed to locomotion
        Vector3 dir = new Vector3(action[0], 0, action[1]).normalized;
        agentController.DesiredDirection = dir;

        // 4) Rewards and termination
        foreach (var r in rewProviders)
        {
            r.Step(out var rwd);
            CumulativeReward += rwd;
            if (r.Done) Done = true;
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

        Rb.velocity = Rb.angularVelocity = Tf.position = Vector3.zero;
        Rb.isKinematic = true;
        coll.enabled = false;
        rend.enabled = false;
        agentController.enabled = false;
    }

    public void Respawn()
    {
        despawned = false;
        Rb.isKinematic = false;
        coll.enabled = true;
        rend.enabled = true;
        agentController.enabled = true;
    }
}