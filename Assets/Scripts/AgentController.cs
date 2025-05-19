using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(Rigidbody))]
public class PlayerAgent : Agent
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float turnSpeed = 720f;

    [Header("Scene refs")]
    public Transform goal;
    public float arenaRadius = 7f; // episode resets if agent leaves

    Rigidbody rb;
    Vector3 desiredDir = Vector3.zero;

    public override void Initialize() => rb = GetComponent<Rigidbody>();

    public override void OnEpisodeBegin()
    {
        // 1) reset velocity
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 2) place agent somewhere inside the arena
        Vector2 pos2D = Random.insideUnitCircle * (arenaRadius * .5f);
        transform.localPosition = new Vector3(pos2D.x, 1f, pos2D.y);

        // 3) optionally move the goal as well (static in this demo)
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 3 floats: agent-to-goal direction (normalized)
        Vector3 toGoal = (goal.position - transform.position).normalized;
        sensor.AddObservation(toGoal);

        // 3 floats: current velocity (local space)
        sensor.AddObservation(transform.InverseTransformDirection(rb.velocity));
    }

    /*------------ actions from the policy (or keyboard in Heuristic) ----*/
    public override void OnActionReceived(ActionBuffers actions)
    {
        desiredDir = new Vector3(actions.ContinuousActions[0], 0,
                                 actions.ContinuousActions[1]).normalized;
        // MoveAgent();

        float distToGoal = Vector3.Distance(transform.position, goal.position);

        AddReward(-0.001f);

        if (distToGoal < 1.0f)
        {
            AddReward(+1f);
            EndEpisode();
        }

        if (transform.position.y < -1 ||
            transform.localPosition.magnitude > arenaRadius)
        {
            AddReward(-1f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = Input.GetAxisRaw("Horizontal");
        ca[1] = Input.GetAxisRaw("Vertical");
    }

    void FixedUpdate() => MoveAgent();

    void MoveAgent()
    {
        Vector3 targetVel = desiredDir * moveSpeed;
        Vector3 velocityChange = targetVel - rb.velocity; velocityChange.y = 0;
        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        if (desiredDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            Quaternion newRot = Quaternion.RotateTowards(rb.rotation,
                                 targetRot, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }
    }
}