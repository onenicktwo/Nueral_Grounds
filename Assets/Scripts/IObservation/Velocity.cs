using UnityEngine;

public class Velocity : IObservation
{
    public IAgentView ag { get; set; }
    public int Size => 2;

    public IObservation Clone() => new Velocity();

    public int Write(float[] buffer, int offset)
    {
        buffer[offset++] = ag.Rb.velocity.x;
        buffer[offset++] = ag.Rb.velocity.z;
        return offset;
    }
}