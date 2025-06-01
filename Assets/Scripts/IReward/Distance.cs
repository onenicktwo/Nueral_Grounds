using UnityEngine;

public class Distance: IReward
{
    public IAgentView ag { get; set; }
    [SerializeField] Transform target;
    Vector3 startPos;
    public bool Done { get; private set; }
    private float radius = 0.5f;
    private int inv = 1;

    public IReward Clone() =>
        new Distance(target, radius, inv == -1);

    public void Reset()
    {
        Done = false;
        startPos = ag.Tf.position;
    }

    public Distance(Transform t, float r, bool isInv)
    {
        target = t;
        radius = r;
        inv = isInv ? -1 : 1;
    }

    public void Step(out float r)
    {
        float d = Vector3.Distance(ag.Tf.position, target.position);
        r = inv * -d * Time.fixedDeltaTime;
        if (d < radius)
        {
            r += 50f;
            Done = true;
        }
    }
}