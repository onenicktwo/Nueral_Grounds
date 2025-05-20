using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
public class Adam
{
    float lr, b1, b2, eps;
    Dictionary<Tensor, (float[,], float[,])> m = new();
    int t = 0;

    public Adam(float lr = 1e-3f, float b1 = 0.9f, float b2 = 0.999f, float eps = 1e-8f)
    {
        this.lr = lr; this.b1 = b1; this.b2 = b2; this.eps = eps;
    }

    public void Step(IEnumerable<Tensor> parameters)
    {
        t++;
        foreach (var p in parameters)
        {
            if (!m.ContainsKey(p))
                m[p] = (new float[p.rows, p.cols], new float[p.rows, p.cols]);

            var (mW, vW) = m[p];
            for (int i = 0; i < p.rows; i++)
                for (int j = 0; j < p.cols; j++)
                {
                    float g = p.Grad[i, j];
                    mW[i, j] = b1 * mW[i, j] + (1 - b1) * g;
                    vW[i, j] = b2 * vW[i, j] + (1 - b2) * g * g;

                    float mHat = mW[i, j] / (1 - math.pow(b1, t));
                    float vHat = vW[i, j] / (1 - math.pow(b2, t));

                    p.W[i, j] -= lr * mHat / (math.sqrt(vHat) + eps);
                }
        }
    }
}