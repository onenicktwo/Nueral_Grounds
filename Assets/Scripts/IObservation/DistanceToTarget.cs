using UnityEngine;

public class DistanceToTarget : MonoBehaviour, IObservation
{
    [SerializeField] Transform target;
    public int Size => 2;  // x,z

    void Awake() => target = GameObject.FindGameObjectWithTag("Goal").transform;

    public int Write(float[] buffer, int offset)
    {
        Vector3 diff = target.position - transform.position;
        buffer[offset++] = diff.x;
        buffer[offset++] = diff.z;
        return offset;
    }
}
