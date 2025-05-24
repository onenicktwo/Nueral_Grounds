using UnityEngine;

public class Distance: MonoBehaviour, IReward
{
    [SerializeField] Transform target;
    Vector3 startPos;
    public bool Done { get; private set; }
    const float Radius = 0.5f;

    void Awake() => target = GameObject.FindGameObjectWithTag("Goal").transform;

    public void Reset()
    {
        Done = false;
        startPos = transform.position;
    }

    public void Step(out float r)
    {
        float d = Vector3.Distance(transform.position, target.position);
        r = -d * Time.fixedDeltaTime;
        if (d < Radius)
        {
            r += 50f;
            Done = true;
        }
    }
}