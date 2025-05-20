using UnityEngine;

public class EnvInstance : MonoBehaviour
{
    [SerializeField] RewardGraph rewardGraph;
    public Transform goal;
    [SerializeField] float resetRadius = 8f;
    [SerializeField] float arenaRadius = 10f;
    [SerializeField] float outOfBoundsPenalty = -1.0f;
    [SerializeField] float spawnMargin = 1f;

    public void ResetEnv(AgentController agent)
    {
        float maxR = arenaRadius - spawnMargin;
        Vector2 rnd = Random.insideUnitCircle * maxR;
        agent.transform.position = new Vector3(rnd.x, 0.5f, rnd.y);
        agent.rb.velocity = Vector3.zero;
    }

    public float ComputeReward(Vector3 pos, ref bool done)
    {
        float d = Vector3.Distance(pos, goal.position);
        float r = rewardGraph.distanceCurve.Evaluate(d) + rewardGraph.stepPenalty;

        if (d < 1f)
        {
            r += rewardGraph.goalBonus;
            done = true;
        }

        if (Vector3.Distance(pos, transform.position) > arenaRadius)
        {
            r += outOfBoundsPenalty; 
            done = true; 
        }

        return r;
    }
}