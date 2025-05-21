using UnityEngine;

public class AgentController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSpeed = 720f;

    [Header("Mode")]
    [Tooltip("If TRUE player keyboard. If FALSE external action (RL/Python).")]
    public bool heuristic = true;

    private Vector3 desiredDir = Vector3.zero;

    private void Update()
    {
        if (heuristic)
        {
            ReadKeyboardInput();
        }

        MoveAgent();
    }

    private void ReadKeyboardInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        desiredDir = new Vector3(h, 0, v).normalized;
    }

    private void MoveAgent()
    {
        if (desiredDir.sqrMagnitude > 0.001f)
        {
            Vector3 move = desiredDir * moveSpeed * Time.deltaTime;
            transform.Translate(move, Space.World);

            Quaternion targetRotation = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}