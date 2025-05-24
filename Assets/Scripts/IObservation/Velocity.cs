using UnityEngine;

public class Velocity : MonoBehaviour, IObservation
{
    Rigidbody rb;
    void Awake() => rb = GetComponent<Rigidbody>();
    public int Size => 2;

    public int Write(float[] buffer, int offset)
    {
        buffer[offset++] = rb.velocity.x;
        buffer[offset++] = rb.velocity.z;
        return offset;
    }
}