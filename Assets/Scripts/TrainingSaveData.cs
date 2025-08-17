using UnityEngine;

[SerializeField]
public class TrainingSaveData : MonoBehaviour
{
    public int generation;
    public float[] masterTheta;
    public IPolicy policyType;
}
