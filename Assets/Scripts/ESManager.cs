using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ESManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Button startBtn;
    [SerializeField] Button stopBtn;
    [SerializeField] Slider popSlider;
    [SerializeField] Text genText;

    [Header("References")]
    [SerializeField] GameObject agentPrefab;
    [SerializeField] Transform agentParent;
    [SerializeField] Transform target;

    [Header("Hyper-parameters")]
    [SerializeField] float sigma = 0.1f;         // exploration noise
    [SerializeField] float alpha = 0.05f;        // learning-rate
    [SerializeField] int inputDim = 4;
    [SerializeField] int outputDim = 2;

    float[] θ;                     // master weights
    int paramCount;
    List<ESAgent> population = new();

    int generation;
    bool running;
    Coroutine trainLoop;

    void Awake()
    {
        paramCount = LinearPolicy.ParamCount(inputDim, outputDim);
        θ = new float[paramCount];

        startBtn.onClick.AddListener(() =>
        {
            if (!running) trainLoop = StartCoroutine(TrainingLoop());
        });
        stopBtn.onClick.AddListener(ResetAll);

        popSlider.value = 50;
        UpdateGenText();
    }

    IEnumerator TrainingLoop()
    {
        running = true;
        while (running)
        {
            int popSize = Mathf.RoundToInt(popSlider.value);
            SpawnPopulation(popSize);
            yield return EvaluatePopulation();   // waits until everyone Done
            UpdateMaster(); // gradient ascent
            Cleanup();
            generation++;
            UpdateGenText();
        }
    }

    void SpawnPopulation(int n)
    {
        population.Clear();
        for (int i = 0; i < n; i++)
        {
            // 1) sample noise
            float[] ε = new float[paramCount];
            for (int k = 0; k < paramCount; k++) ε[k] = Random.Range(-1f, 1f);

            // 2) θ_i = θ + sig * epsil
            float[] θ_i = new float[paramCount];
            for (int k = 0; k < paramCount; k++) θ_i[k] = θ[k] + sigma * ε[k];

            // 3) store epsil as we’ll need it to compute gradient
            GameObject go = Instantiate(agentPrefab, agentParent);
            ESAgent agent = go.GetComponent<ESAgent>();
            agent.Init(θ_i, target.position, inputDim, outputDim);
            population.Add(agent);
            noiseBank.Add(ε);
        }
    }

    // stores noise for gradient calc
    List<float[]> noiseBank = new();

    IEnumerator EvaluatePopulation()
    {
        // naive: just wait until every agent signals Done
        bool allDone;
        do
        {
            allDone = true;
            foreach (var a in population)
                if (!a.Done) { allDone = false; break; }
            yield return null;
        } while (!allDone);
    }

    void UpdateMaster()
    {
        // canonical ES gradient estimate
        int n = population.Count;
        float[] grad = new float[paramCount];

        // normalise returns (rank or std) – here: simple mean/std
        float mean = 0, var = 0;
        foreach (var a in population) mean += a.Fitness;
        mean /= n;
        foreach (var a in population) var += Mathf.Pow(a.Fitness - mean, 2);
        float std = Mathf.Sqrt(var / n) + 1e-8f;

        for (int i = 0; i < n; i++)
        {
            float normR = (population[i].Fitness - mean) / std;
            float[] ε = noiseBank[i];
            for (int k = 0; k < paramCount; k++)
                grad[k] += normR * ε[k];
        }
        for (int k = 0; k < paramCount; k++)
            θ[k] += (alpha / (n * sigma)) * grad[k];   // gradient ascent step
    }

    void Cleanup()
    {
        foreach (var a in population)
            Destroy(a.gameObject);
        noiseBank.Clear();
    }

    void ResetAll()
    {
        if (trainLoop != null) StopCoroutine(trainLoop);
        Cleanup();
        running = false;
        generation = 0;
        θ = new float[paramCount];
        UpdateGenText();
    }
    void UpdateGenText() => genText.text = $"Gen: {generation}";
}