using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class A2CLearner
{
    // Hyper-parameters
    public float gamma = 0.99f;
    public float entropyB = 0.01f;
    public float lr = 3e-4f;

    // Networks
    TinyNN policy;       // outputs mean (actionDim)
    TinyNN value;        // outputs V(s)

    // Log-std parameters (1 per action dim)
    public float[] logStd;

    // Optimiser
    Adam optim;

    public int obsDim, actDim;

    public A2CLearner(int obsDim, int actDim)
    {
        this.obsDim = obsDim;
        this.actDim = actDim;

        policy = new TinyNN(obsDim, new[] { 64, 64 }, actDim);
        value = new TinyNN(obsDim, new[] { 64, 64 }, 1);
        logStd = new float[actDim];
        optim = new Adam(lr);

        for (int i = 0; i < actDim; ++i)
            logStd[i] = 0.5f;
    }

    // Forward policy returns action and log-prob
    public (float[] action, float logp) SampleAction(float[] obs, Random rng)
    {
        var actsPolicy = policy.Forward(obs, applyTanhLast: true);    // tanh in last layer
        var mu = actsPolicy[^1];
        float[] std = new float[actDim];
        for (int i = 0; i < actDim; i++)
            std[i] = exp(logStd[i]);

        float[] a = new float[actDim];
        float logp = 0;
        for (int i = 0; i < actDim; i++)
        {
            float eps = RLUtils.NextFloatNormal(ref rng);
            a[i] = mu[i] + std[i] * eps;
            // log N
            logp += -0.5f * (pow((a[i] - mu[i]) / std[i], 2) + 2 * logStd[i] + log(2 * PI));
        }
        return (a, logp);
    }

    public float SampleValue(float[] obs)
    {
        return value.Forward(obs)[^1][0];
    }

    struct Step
    {
        public float[] s, a;
        public float r;
        public float logp;
        public float done;
        public float value;
    }
    List<Step> buffer = new();

    public void AddTransition(float[] s, float[] a, float r, float logp, float v, bool done)
    {
        buffer.Add(new Step { s = s, a = a, r = r, logp = logp, value = v, done = done ? 1f : 0f });
    }

    public void Learn()
    {
        int T = buffer.Count;
        if (T == 0) return;

        // Bootstrap value of last state
        float[] lastS = buffer[^1].s;
        float bootV = value.Forward(lastS)[^1][0];

        float[] returns = new float[T];
        float running = bootV;

        for (int t = T - 1; t >= 0; t--)
        {
            running = buffer[t].r + gamma * running * (1 - buffer[t].done);
            returns[t] = running;
        }

        policy.ZeroGrad();
        value.ZeroGrad();

        float actorLoss = 0, criticLoss = 0, entLoss = 0;

        for (int t = 0; t < T; t++)
        {
            var step = buffer[t];
            float[] vPredArr = value.Forward(step.s)[^1];
            float vPred = vPredArr[0];
            float adv = returns[t] - vPred;

            // value network backward (MSE)
            value.Backward(value.Forward(step.s), new float[] { -2 * adv });

            // policy backward
            actorLoss += -step.logp * adv;

            // entropy (Gaussian): sum(log std + 0.5*log 2pie)
            entLoss += 0.5f * (2 * logStd.Length + 2 * Sum(logStd) + log(2 * PI));
        }

        float totalLoss = actorLoss + 0.5f * criticLoss + entropyB * entLoss;

        // accumulate grads were already placed during backward
        optim.Step(GetAllParams());

        buffer.Clear();
    }

    IEnumerable<Tensor> GetAllParams()
    {
        foreach (var t in policy.layers) yield return t;
        foreach (var t in value.layers) yield return t;
    }

    float Sum(float[] arr) { float s = 0; foreach (var x in arr) s += x; return s; }
}