public static class LinearPolicy
{
    // y = W*x + b      (flattens weights into W + b)
    public static float[] Forward(float[] weights, float[] obs, int input, int output)
    {
        float[] act = new float[output];
        int idx = 0;
        for (int o = 0; o < output; o++)
        {
            float sum = 0;
            for (int i = 0; i < input; i++) sum += weights[idx++] * obs[i];
            sum += weights[idx++];
            act[o] = sum;
        }
        return act;
    }

    public static int ParamCount(int input, int output) => (input + 1) * output;
}