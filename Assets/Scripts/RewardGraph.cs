using UnityEngine;

[CreateAssetMenu]
public class RewardGraph : ScriptableObject
{
    public AnimationCurve distanceCurve = AnimationCurve.Linear(0, 1, 10, 0);
    public float stepPenalty = -0.005f;
    public float goalBonus = 2f;
}