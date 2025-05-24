using UnityEngine;

[RequireComponent(typeof(AgentController))]
public class ESAgent : MonoBehaviour
{
    AgentController agentController;
    public void Init(IPolicy policy, float[] θ)
    {
        _policy = policy;
        _policy.SetParams(θ);
        _obsProviders = GetComponents<IObservation>();
        _rewProviders = GetComponents<IReward>();
        agentController = GetComponent<AgentController>();

        _buffer = new float[_policy.InputDim];
        _action = new float[_policy.OutputDim];

        _despawned = false;

        foreach (var r in _rewProviders) r.Reset();
        CumulativeReward = 0;
        Done = false;
        timer = 0f;
    }

    IObservation[] _obsProviders;
    IReward[] _rewProviders;
    IPolicy _policy;

    float[] _buffer;
    float[] _action;

    const float MaxEpisodeTime = 3f;
    float timer;

    bool _despawned;

    public float CumulativeReward { get; private set; }
    public bool Done { get; private set; }

    void FixedUpdate()
    {
        if (Done) return;

        // 1) Build observation buffer
        int offset = 0;
        foreach (var o in _obsProviders)
            offset = o.Write(_buffer, offset);

        // 2) Policy -> action
        _policy.Act(_buffer, _action);

        // 3) Feed to locomotion / animation
        Vector3 dir = new Vector3(_action[0], 0, _action[1]).normalized;
        agentController.DesiredDirection = dir;

        // 4) Rewards & termination
        foreach (var r in _rewProviders)
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
        if (_despawned) return;
        _despawned = true;

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