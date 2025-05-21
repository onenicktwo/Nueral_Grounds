using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ESManager : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] Transform goal;
    [SerializeField] AgentPool pool;

    [Header("UI refs")]
    [SerializeField] Slider popSlider;
    [SerializeField] Button startBtn;
    [SerializeField] Button resetBtn;
    [SerializeField] Text genText;

    float[] mu = new float[ESAgent.PARAM];
    float[] std = Enumerable.Repeat(1f, ESAgent.PARAM).ToArray();

    int generation;
    bool isTraining;

    public delegate void EpisodeEvent(ESAgent ag, float score);
    event EpisodeEvent OnEpisodeFinished;

    void Awake()
    {
        startBtn.onClick.AddListener(() =>
        {
            if (!isTraining) StartCoroutine(TrainLoop());
        });
        resetBtn.onClick.AddListener(ResetTraining);
    }

    int PopSize => (int)popSlider.value;
    int Elite => Mathf.Max(2, PopSize / 8);

    IEnumerator TrainLoop()
    {
        isTraining = true;
        generation = 0;

        while (isTraining)
        {
            generation++;
            genText.text = $"Gen {generation} / Pop {PopSize}";
            var results = new List<(float[] w, float score)>();

            // 1. Sample population
            for (int i = 0; i < PopSize; i++)
            {
                float[] w = SampleWeights();
                ESAgent ag = pool.Get();
                PositionAgent(ag, i);

                // subscribe once
                void Handler(ESAgent a, float s)
                {
                    if (a != ag) return;
                    results.Add((w, s));
                    OnEpisodeFinished -= Handler;
                    pool.Recycle(a);
                }
                OnEpisodeFinished += Handler;

                ag.Init(this, goal, w);
            }

            // 2. Wait until all results are back
            while (results.Count < PopSize) yield return null;

            // 3. Elite select + update
            results.Sort((a, b) => b.score.CompareTo(a.score));
            var elite = results.Take(Elite).ToArray();

            for (int p = 0; p < ESAgent.PARAM; p++)
            {
                mu[p] = elite.Average(e => e.w[p]);
                std[p] = Mathf.Sqrt(elite.Average(e => Mathf.Pow(e.w[p] - mu[p], 2)));
            }
        }
    }

    public void EpisodeFinished(ESAgent ag, float score) =>
        OnEpisodeFinished?.Invoke(ag, score);

    float[] SampleWeights()
    {
        var w = new float[ESAgent.PARAM];
        for (int i = 0; i < w.Length; i++)
            w[i] = mu[i] + std[i] * RandomNormal();
        return w;
    }

    static float RandomNormal()
    {
        // Box-Muller polar
        float u1 = 1f - Random.value;
        float u2 = 1f - Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
    }

    void PositionAgent(ESAgent ag, int idx)
    {
        float radius = 5f;
        float angle = idx * Mathf.PI * 2f / PopSize;
        Vector3 pos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
        ag.transform.position = pos;
    }

    void ResetTraining()
    {
        StopAllCoroutines();
        isTraining = false;
        generation = 0;
        mu = new float[ESAgent.PARAM];
        std = Enumerable.Repeat(1f, ESAgent.PARAM).ToArray();
        genText.text = "Ready";

        // deactivate any live agents
        foreach (Transform child in pool.transform)
            child.gameObject.SetActive(false);
    }
}