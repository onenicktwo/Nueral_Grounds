using UnityEngine;

[RequireComponent(typeof(ESAgent))]
[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveForce = 20f;
    [SerializeField] float maxSpeed = 100f;
    [SerializeField] float turnSpeed = 720f;

    Rigidbody rb;
    ESAgent esAgent;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        esAgent = GetComponent<ESAgent>();
        if (rb == null) Debug.LogError("AgentController requires a Rigidbody component!");
        if (esAgent == null) Debug.LogError("AgentController requires an ESAgent component!");
    }

    void FixedUpdate()
    {
        if (esAgent == null) return;

        Vector3 desiredDir = esAgent.DesiredDirection;

        if (desiredDir.sqrMagnitude > 0.001f)
        {
            // Movement
            rb.AddForce(desiredDir * moveForce);

            // Rotation
            Quaternion targetRotation = Quaternion.LookRotation(desiredDir, Vector3.up);
            Quaternion newRotation = Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRotation); 
        }

        if (rb.velocity.magnitude > maxSpeed)
        {
             rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }
}