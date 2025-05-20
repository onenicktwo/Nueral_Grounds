using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveForce = 8f;
    [SerializeField] float turnSpeed = 720f;

    [Header("Mode")]
    public bool heuristic = false;      // default to learning

    [SerializeField]
    public Rigidbody rb;
    Vector3 desiredDir;                 // from keyboard
    GameManager mgr;                  // set by manager
    Unity.Mathematics.Random rng;

    int episodeCount;
    public Material defaultMat, happyMat, sadMat;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rng = new Unity.Mathematics.Random((uint)(GetInstanceID() * 13 + 1));
    }

    public void Init(GameManager m) => mgr = m;

    void Update()
    {
        if (heuristic) ReadKeyboard();
    }

    void FixedUpdate()
    {
        float[] obs = CollectObs();
        float[] action;
        float logp = 0;

        if (heuristic)
        {
            action = new[] { desiredDir.x, desiredDir.z };
        }
        else
        {
            (action, logp) = mgr.learner.SampleAction(obs, rng);
        }

        // execute
        Vector3 force = new Vector3(action[0], 0, action[1]) * moveForce;
        rb.AddForce(force, ForceMode.Acceleration);
        if (lengthsq(force) > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(new Vector3(action[0], 0, action[1]), Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
        }

        // reward
        bool done = false;
        float r = mgr.env.ComputeReward(transform.position, ref done);

        mgr.learner.AddTransition(obs, action, r, logp, mgr.learner.SampleValue(obs), done);

        if (done)
        {
            OnEpisodeEnd(r > 0);
            mgr.env.ResetEnv(this);
        }
    }

    float[] CollectObs()
    {
        Vector3 toGoal = mgr.env.goal.position - transform.position;
        return new float[]
        {
            toGoal.x / 10f,
            toGoal.z / 10f,
            rb.velocity.x / 5f,
            rb.velocity.z / 5f
        }; // obsDim = 4
    }

    void ReadKeyboard()
    {
        desiredDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;
    }

    public void OnEpisodeEnd(bool success)
    {
        episodeCount++;
        GetComponent<Renderer>().material = success ? happyMat : sadMat;
        Invoke(nameof(RestoreMat), 0.2f);
    }

    void RestoreMat() => GetComponent<Renderer>().material = defaultMat;
}