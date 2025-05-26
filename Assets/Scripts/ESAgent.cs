using UnityEngine;

[RequireComponent(typeof(AgentController))]
public class ESAgent : MonoBehaviour
{
    AgentController agentController;
    public void Init(IPolicy _policy, float[] θ)
    {
        policy = _policy;
        policy.SetParams(θ);
        obsProviders = GetComponents<IObservation>();
        rewProviders = GetComponents<IReward>();
        agentController = GetComponent<AgentController>();

        buffer = new float[policy.InputDim];
        action = new float[policy.OutputDim];

        despawned = false;

        foreach (var r in rewProviders) r.Reset();
        CumulativeReward = 0;
        Done = false;
        timer = 0f;
    }

    IObservation[] obsProviders;
    IReward[] rewProviders;
    IPolicy policy;

    float[] buffer;
    float[] action;

    const float MaxEpisodeTime = 3f;
    float timer;

    bool despawned;

    public float CumulativeReward { get; private set; }
    public bool Done { get; private set; }

    void FixedUpdate()
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
                Despawn();
            }
        }

        timer += Time.fixedDeltaTime;
        if (timer > MaxEpisodeTime)
            Done = true;
    }

    void Despawn()
    {
        if (despawned) return;
        despawned = true;

        var rb = GetComponent<Rigidbody>();
        rb.velocity = rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        agentController.enabled = false;

        foreach (var ren in GetComponentsInChildren<Renderer>())
            ren.enabled = false;
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;
    }
}