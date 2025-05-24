using UnityEngine;

public class LinearPolicy : IPolicy
{
    public int InputDim { get; }
    public int OutputDim { get; }
    public int ParamCount => (InputDim + 1) * OutputDim;

    public LinearPolicy(int inDim, int outDim)
    {
        InputDim = inDim;
        OutputDim = outDim;
        _theta = new float[ParamCount];
    }

    float[] _theta;

    public void SetParams(float[] theta)
    {
        if (theta.Length != ParamCount)
            Debug.LogError($"theta length mismatch: got {theta.Length}, need {ParamCount}");
        _theta = theta;
    }

    public float[] GetParams() => _theta;

    public void Act(float[] obs, float[] act)
    {
        int p = 0;
        for (int o = 0; o < OutputDim; o++)
        {
            float sum = 0f;
            for (int i = 0; i < InputDim; i++)
                sum += _theta[p++] * obs[i];

            sum += _theta[p++];
            act[o] = sum;
        }
    }
}