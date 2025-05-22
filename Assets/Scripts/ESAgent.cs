using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class ESAgent : MonoBehaviour
{
    public const int OBS = 4; // observations: dx, dz, vx, vz
    public const int ACT = 2; // actions: x, z normalized dir
    public const int PARAM = OBS * ACT;

    float[] weights;

    public Vector3 DesiredDirection { get; private set; }

    ESManager trainer;
    Transform goal;
    Rigidbody rb;

    int step, maxStep = 200;
    float episodeReturn;

    public void Init(ESManager mgr, Transform goalTf, float[] w)
    {
        trainer = mgr;
        goal = goalTf;
        weights = w;

        step = 0;
        episodeReturn = 0f;
        DesiredDirection = Vector3.zero;

        if (!rb) rb = GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;

        StartCoroutine(RunEpisode());
    }

    IEnumerator RunEpisode()
    {
        while (step < maxStep)
        {
            step++;

            float[] obs = GetObs();
            Vector2 act = LinearPolicy(obs);
            DesiredDirection = new Vector3(act.x, 0, act.y).normalized;

            yield return new WaitForFixedUpdate();

            bool done = Vector3.Distance(transform.position, goal.position) < 0.6f;
            episodeReturn += done ? +1f : -0.005f; // reward

            if (done) break;
        }

        trainer.EpisodeFinished(this, episodeReturn);
    }

    float[] GetObs()
    {
        Vector3 toGoal = goal.position - transform.position;
        float vx = rb ? rb.velocity.x : 0f;
        float vz = rb ? rb.velocity.z : 0f;
        return new[] { toGoal.x, toGoal.z, vx, vz };
    }

    Vector2 LinearPolicy(float[] o)
    {
        float vx_policy = 0, vz_policy = 0;
        for (int i = 0; i < OBS; i++)
        {
            vx_policy += weights[i] * o[i];
            vz_policy += weights[i + OBS] * o[i];
        }

        return new Vector2(tanh(vx_policy), tanh(vz_policy));
    }
}