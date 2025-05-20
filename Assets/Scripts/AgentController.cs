using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSpeed = 720f;

    [Header("Mode")]
    [Tooltip("If TRUE player keyboard. If FALSE external action (RL/Python).")]
    public bool heuristic = true;

    private Rigidbody rb;
    private Vector3 desiredDir = Vector3.zero;

    private void Awake() => rb = GetComponent<Rigidbody>();

    private void Update()
    {
        if (!heuristic) return;
        ReadKeyboardInput();
    }

    private void FixedUpdate()
    {
        MoveAgent();
    }

    public void EnableHeuristic() => heuristic = true;

    private void ReadKeyboardInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        desiredDir = new Vector3(h, 0, v).normalized;
    }

    private void MoveAgent()
    {
        Vector3 targetVel = desiredDir * moveSpeed;
        Vector3 velocityChange = targetVel - rb.velocity;
        velocityChange.y = 0;
        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        if (desiredDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }
    }
}