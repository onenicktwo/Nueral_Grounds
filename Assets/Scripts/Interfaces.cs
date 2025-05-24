public interface IObservation
{
    int Size { get; }
    int Write(float[] buffer, int offset);
}

public interface IReward
{
    void Reset();
    void Step(out float reward);
    bool Done { get; }
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