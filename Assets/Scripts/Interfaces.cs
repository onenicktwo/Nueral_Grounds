using UnityEngine;

public interface IObservation : IDeepClone<IObservation>
{
    int Size { get; }
    int Write(float[] buffer, int offset);

    IAgentView ag { set; get; }

    public void initAgent(IAgentView view)
    {
        ag = view;
    }
}

public interface IReward : IDeepClone<IReward>
{
    void Reset();
    void Step(out float reward);
    bool Done { get; }
    float RewardMultiplier { get; }

    IAgentView ag { get; set; }
}

public interface IPolicy
{
    int InputDim { get; }
    int OutputDim { get; }
    int ParamCount { get; } 

    void SetParams(float[] theta);
    float[] GetParams();

    void Act(float[] obs, float[] act);
}

public interface IAgentView
{
    Transform Tf { get; }
    Rigidbody Rb { get; }
}

public interface IDeepClone<T>
{
    T Clone();
}