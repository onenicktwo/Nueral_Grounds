using Unity.Mathematics;
public class Tensor
{
    public float[,] W;
    public float[,] Grad;

    public int rows => W.GetLength(0);
    public int cols => W.GetLength(1);

    public Tensor(int r, int c, float std = 0.02f)
    {
        W = new float[r, c];
        Grad = new float[r, c];
        var rng = new Random(1234);
        for (int i = 0; i < r; i++)
            for (int j = 0; j < c; j++)
                W[i, j] = rng.NextFloat() * std;
    }

    public void ZeroGrad()
    {
        System.Array.Clear(Grad, 0, Grad.Length);
    }
}