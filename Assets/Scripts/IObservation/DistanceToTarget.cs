using UnityEngine;

public class DistanceToTarget : IObservation
{
    public IAgentView ag { get; set; }
    Transform target;
    public int Size => 2;  // x,z

    public IObservation Clone() => new DistanceToTarget(target);

    public int Write(float[] buffer, int offset)
    {
        Vector3 diff = target.position - ag.Tf.position;
        buffer[offset++] = diff.x;
        buffer[offset++] = diff.z;
        return offset;
    }

    public DistanceToTarget(Transform transform)
    {
        target = transform;
    }
}
