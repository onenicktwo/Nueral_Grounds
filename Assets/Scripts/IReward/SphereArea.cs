using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereArea : IReward
{
    public IAgentView ag { get; set; }
    [SerializeField] Transform transform;
    public bool Done { get; private set; }
    private float radius = 0.5f;
    private int inv = 1;
    private bool destroyOnEnter = false;

    // A little hack to make a interface var private
    float IReward.RewardMultiplier => rewardMultiplier;
    private float rewardMultiplier { get; set; }

    public IReward Clone() =>
        new SphereArea(rewardMultiplier, transform, radius, inv == -1, destroyOnEnter);

    public void Reset()
    {
        Done = false;
    }

    public SphereArea(float rewardMultiplier, Transform transform, float radius, bool isInv, bool destroyOnEnter)
    {
        this.rewardMultiplier = rewardMultiplier;
        this.transform = transform;
        this.radius = radius;
        inv = isInv ? -1 : 1;
        this.destroyOnEnter = destroyOnEnter;
    }

    public void Step(out float r)
    {
        float d = Vector3.Distance(ag.Tf.position, transform.position);
        if (d < radius)
        {
            r = rewardMultiplier * inv;
            Done = true;
        } else
        {
            r = 0f;
        }
    }
}
