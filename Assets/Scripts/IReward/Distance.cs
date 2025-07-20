using UnityEngine;

public class Distance: IReward
{
    public IAgentView ag { get; set; }
    [SerializeField] Transform target;
    public bool Done { get; private set; }
    private int inv = 1;
    float IReward.RewardMultiplier => rewardMultiplier;
    private float rewardMultiplier { get; set; }

    public IReward Clone() =>
        new Distance(rewardMultiplier, target, inv == -1);

    public void Reset()
    {
        Done = false;
    }

    public Distance(float rewardMultiplier, Transform target, bool isInv)
    {
        this.rewardMultiplier = rewardMultiplier;
        this.target = target;
        inv = isInv ? -1 : 1;
    }

    public void Step(out float r)
    {
        float d = Vector3.Distance(ag.Tf.position, target.position);
        r = inv * -d * Time.fixedDeltaTime;
    }
}