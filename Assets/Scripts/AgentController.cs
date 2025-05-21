using UnityEngine;

[RequireComponent(typeof(ESAgent))]
public class AgentController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float turnSpeed = 720f;

    Vector3 desiredDir;

    void Update()
    {
        desiredDir = GetComponent<ESAgent>().DesiredDirection;

        if (desiredDir.sqrMagnitude > 0.001f)
        {
            Vector3 move = desiredDir * moveSpeed * Time.deltaTime;
            transform.Translate(move, Space.World);

            Quaternion target = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}