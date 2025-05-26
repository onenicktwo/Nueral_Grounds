using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    [SerializeField] GameObject agentPrefab; // Not an actual prefab, but acts like one
    [SerializeField] Transform agentParent;
    [SerializeField] Transform target;

    [Header("Hyper-parameters")]
    [SerializeField] float sigma = 0.1f;         // exploration noise
    [SerializeField] float alpha = 0.05f;        // learning-rate
    int inputDim = 0;
    [SerializeField]  int outputDim = 2; // for now assume only x,z movement

    public MonoBehaviour[] obsProviders;
    public MonoBehaviour[] rewProviders;

    IPolicy masterPolicy;
    float[] theta;
    int paramCount;
    readonly List<ESAgent> population = new();
    readonly List<float[]> noiseBank = new();

    int generation;
    bool running;
    Coroutine trainLoop;

    public static ESManager instance;

    void Awake()
    {
        startBtn.onClick.AddListener(() =>
        {
            if (!running) Init();
        });
        stopBtn.onClick.AddListener(ResetAll);

        popSlider.value = 50;
        UpdateGenText();

        instance = this;
    }

    void Init()
    {
        foreach (IObservation obsProvider in obsProviders)
            inputDim += obsProvider.Size;

        masterPolicy = new LinearPolicy(inputDim, outputDim); // will be changed for more complex environments
        paramCount = masterPolicy.ParamCount;
        theta = new float[paramCount];

        trainLoop = StartCoroutine(TrainingLoop());
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
        noiseBank.Clear();

        for (int i = 0; i < n; i++)
        {
            float[] epsilon = new float[paramCount];
            for (int k = 0; k < paramCount; k++) epsilon[k] = Random.Range(-1f, 1f);

            float[] theta_i = new float[paramCount];
            for (int k = 0; k < paramCount; k++) theta_i[k] = theta[k] + sigma * epsilon[k];

            GameObject go = Instantiate(agentPrefab, agentParent);
            foreach (var obsProvider in obsProviders)
                go.AddComponent(obsProvider.GetType());
            foreach (var rewProvider in rewProviders)
                go.AddComponent(rewProvider.GetType());
            ESAgent agent = go.GetComponent<ESAgent>();
            agent.Init(new LinearPolicy(inputDim, outputDim), theta_i); // will be changed for complex 
            population.Add(agent);
            noiseBank.Add(epsilon);
        }
    }

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
        foreach (var a in population) mean += a.CumulativeReward;
        mean /= n;
        foreach (var a in population) var += Mathf.Pow(a.CumulativeReward - mean, 2);
        float std = Mathf.Sqrt(var / n) + 1e-8f;

        for (int i = 0; i < n; i++)
        {
            float normR = (population[i].CumulativeReward - mean) / std;
            float[] epsilon = noiseBank[i];
            for (int k = 0; k < paramCount; k++)
                grad[k] += normR * epsilon[k];
        }
        for (int k = 0; k < paramCount; k++)
            theta[k] += (alpha / (n * sigma)) * grad[k];   // gradient ascent step

        masterPolicy.SetParams(theta);
    }

    void Cleanup()
    {
        foreach (var a in population)
            Destroy(a.gameObject);
        noiseBank.Clear();
        population.Clear();
    }

    void ResetAll()
    {
        if (trainLoop != null) StopCoroutine(trainLoop);
        Cleanup();
        running = false;
        generation = 0;
        theta = new float[paramCount];
        masterPolicy.SetParams(theta);

        UpdateGenText();
    }
    void UpdateGenText() => genText.text = $"Gen: {generation}";
}