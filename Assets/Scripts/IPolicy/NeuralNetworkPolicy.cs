using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
 Tested against LinearPolicy, converged much faster on simple and has the potential to converge fast on complex.
 */
public class NeuralNetworkPolicy : IPolicy
{
    private int HiddenDim;

    public int InputDim { get; }
    public int OutputDim { get; }
    public int ParamCount => (InputDim + 1) * HiddenDim + (HiddenDim + 1) * OutputDim;

    private float[] _theta;

    private float[] _hiddenBuffer;

    public NeuralNetworkPolicy(int inDim, int outDim, int hiddenDim = 20)
    {
        InputDim = inDim;
        OutputDim = outDim;
        HiddenDim = hiddenDim;

        _theta = new float[ParamCount];
        _hiddenBuffer = new float[HiddenDim];
    }

    public void SetParams(float[] theta)
    {
        Array.Copy(theta, _theta, theta.Length);
    }

    public float[] GetParams() => _theta;

    public void Act(float[] obs, float[] act)
    {
        int p = 0;

        // Input -> Hidden (ReLU)
        for (int h = 0; h < HiddenDim; h++)
        {
            float sum = 0f;

            // Weights
            for (int i = 0; i < InputDim; i++)
            {
                sum += obs[i] * _theta[p++];
            }

            // Bias
            sum += _theta[p++];

            // ReLU (Rectified Linear Unit)
            _hiddenBuffer[h] = sum > 0 ? sum : 0;
        }

        // Hidden -> Output (Tanh)
        for (int o = 0; o < OutputDim; o++)
        {
            float sum = 0f;

            // Weights
            for (int h = 0; h < HiddenDim; h++)
            {
                sum += _hiddenBuffer[h] * _theta[p++];
            }

            // Bias
            sum += _theta[p++];

            // Tanh (Hyperbolic Tangent)
            // Squashes result to range [-1, 1] for movement physics.
            act[o] = (float)System.Math.Tanh(sum);
        }
    }
}
