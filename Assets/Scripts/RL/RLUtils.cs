using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class RLUtils
{
    // standard-normal Box–Muller
    public static float NextFloatNormal(ref this Random rng)
    {
        float u1 = clamp(rng.NextFloat(), 1e-7f, 0.9999999f);
        float u2 = rng.NextFloat();
        return sqrt(-2f * log(u1)) * sin(2f * PI * u2);
    }
}