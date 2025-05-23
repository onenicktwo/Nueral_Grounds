using UnityEngine;

[RequireComponent(typeof(AgentController))]
public class ESAgent : MonoBehaviour
{
    AgentController mover;
    Vector3 targetPos;

    float[] weights;
    int inputDim, outputDim;

    public float Fitness { get; private set; }
    public bool Done { get; private set; }

    const float MaxEpisodeTime = 10f;
    float timer;

    public void Init(float[] w, Vector3 target, int inDim, int outDim)
    {
        weights = w;
        targetPos = target;
        inputDim = inDim;
        outputDim = outDim;
        mover = GetComponent<AgentController>();
        mover.ResetAgent(Vector3.zero);
        Done = false;
        timer = 0f;
        Fitness = 0f;
    }

    void FixedUpdate()
    {
        if (Done) return;

        // 1) Build observation  (distance + own velocity)
        Vector3 toTgt = (targetPos - transform.position);
        float[] obs = { toTgt.x, toTgt.z, mover.GetComponent<Rigidbody>().velocity.x, mover.GetComponent<Rigidbody>().velocity.z };

        // 2) Policy -> action
        float[] act = LinearPolicy.Forward(weights, obs, inputDim, outputDim);
        Vector3 dir = new Vector3(act[0], 0, act[1]).normalized;
        mover.DesiredDirection = dir;

        // 3) Incremental reward  (negative distance each physics step)
        Fitness -= toTgt.magnitude * Time.fixedDeltaTime;

        // 4) Terminate?
        timer += Time.fixedDeltaTime;
        if (timer > MaxEpisodeTime)
            Done = true;
    }
}