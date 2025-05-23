using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveForce = 20f;
    [SerializeField] float maxSpeed = 10f;
    [SerializeField] float turnSpeed = 720f;

    Rigidbody rb;
    public Vector3 DesiredDirection { get; set; }

    void Awake() => rb = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        if (DesiredDirection.sqrMagnitude > 0.001f)
        {
            rb.AddForce(DesiredDirection * moveForce, ForceMode.Acceleration);
            Quaternion look = Quaternion.LookRotation(DesiredDirection, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, look, turnSpeed * Time.fixedDeltaTime));
        }
        if (rb.velocity.magnitude > maxSpeed)
            rb.velocity = rb.velocity.normalized * maxSpeed;
    }

    public void ResetAgent(Vector3 pos)
    {
        rb.velocity = rb.angularVelocity = Vector3.zero;
        transform.position = pos;
        DesiredDirection = Vector3.zero;
    }
}