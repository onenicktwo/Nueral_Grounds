using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class TinyNN
{
    public List<Tensor> layers = new();

    public TinyNN(int inDim, int[] hidden, int outDim)
    {
        int prev = inDim;
        foreach (var h in hidden)
        {
            layers.Add(new Tensor(prev, h));
            prev = h;
        }
        layers.Add(new Tensor(prev, outDim));
    }

    // Forward pass returns activations per layer (needed for back-prop)
    public List<float[]> Forward(float[] x, bool applyTanhLast = false)
    {
        var acts = new List<float[]>() { x };
        var inp = x;
        for (int l = 0; l < layers.Count; l++)
        {
            var W = layers[l].W;
            int outDim = W.GetLength(1);
            var outv = new float[outDim];

            for (int j = 0; j < outDim; j++)
            {
                float s = 0;
                for (int i = 0; i < inp.Length; i++)
                    s += inp[i] * W[i, j];
                outv[j] = s;
            }

            // Non-linearities
            if (l < layers.Count - 1)
            {
                for (int j = 0; j < outv.Length; j++)
                    outv[j] = max(0, outv[j]);        // ReLU
            }
            else if (applyTanhLast)
            {
                for (int j = 0; j < outv.Length; j++)
                    outv[j] = tanh(outv[j]);           // continuous action squashing
            }

            acts.Add(outv);
            inp = outv;
        }
        return acts;
    }

    // Simple back-prop with MSE loss (“pred” is last activation)
    public void Backward(List<float[]> acts, float[] gradLast)
    {
        var grad = gradLast;
        for (int l = layers.Count - 1; l >= 0; l--)
        {
            var W = layers[l];

            var input = acts[l];
            int inDim = input.Length;
            int outDim = grad.Length;

            // Weight gradients
            for (int i = 0; i < inDim; i++)
                for (int j = 0; j < outDim; j++)
                    W.Grad[i, j] += input[i] * grad[j];

            // Propagate to previous layer if not first hidden
            if (l > 0)
            {
                var newGrad = new float[inDim];
                for (int i = 0; i < inDim; i++)
                {
                    float s = 0;
                    for (int j = 0; j < outDim; j++)
                        s += grad[j] * W.W[i, j];
                    // ReLU derivative
                    newGrad[i] = acts[l][i] > 0 ? s : 0;
                }
                grad = newGrad;
            }
        }
    }

    public void ZeroGrad()
    {
        foreach (var t in layers) t.ZeroGrad();
    }
}