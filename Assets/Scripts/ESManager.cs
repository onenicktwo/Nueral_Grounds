using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ESManager : MonoBehaviour, Algorithm
{
    [Header("References")]
    [SerializeField] GameObject agentPrefab; // Not an actual prefab, but acts like one
    [SerializeField] Transform agentParent;
    [SerializeField] Transform target;

    [Header("Hyper-parameters")]
    [SerializeField] float sigma = 0.01f;         // exploration noise
    [SerializeField] float alpha = 0.05f;        // learning-rate
    int inputDim = 0;
    [SerializeField]  int outputDim = 2; // for now assume only x,z movement
    [SerializeField] int hiddenDim = 20; // only used for NN policies

    public List<IObservation> obsProviders = new();
    public List<IReward> rewProviders = new();

    IPolicy masterPolicy;
    float[] masterTheta;
    int paramCount;

    private List<ESAgent> allAgents = new List<ESAgent>();
    private Queue<ESAgent> availableAgents = new();
    readonly List<ESAgent> population = new();
    readonly List<float[]> noiseBank = new();

    int generation;
    bool isRunning;
    Coroutine trainLoop;
    int popSize;

    public event Action<int> OnGenerationFinished;

    [SerializeField]
    private float trainingTimeScale = 5f;
    float prevTimeScale;

    void Awake()
    {
        // Testing a fake menu
        obsProviders.Add(new DistanceToTarget(target));
        obsProviders.Add(new Velocity());
        rewProviders.Add(new Distance(1f, target, false));
        rewProviders.Add(new SphereArea(50f, target, 0.5f, false, true));

        prevTimeScale = Time.timeScale;

        for (int i = 0; i < GameManager.I.maxAgents; i++)
        {
            GameObject go = Instantiate(agentPrefab, agentParent);
            ESAgent agent = go.GetComponent<ESAgent>();
            allAgents.Add(agent);
            ReturnToPool(agent);
        }
    }

    public void StartTraining(int populationSize)
    {
        if (isRunning) return;

        popSize = populationSize;
        if (populationSize % 2 != 0) popSize++;

        InitializeTrainingSession();

        trainLoop = StartCoroutine(TrainingLoop());
    }

    public void StopTraining()
    {
        if (trainLoop != null) StopCoroutine(trainLoop);
        Cleanup();
        isRunning = false;
        generation = 0;
        masterTheta = null;
        Time.timeScale = prevTimeScale;
    }

    void InitializeTrainingSession()
    {
        inputDim = 0;
        foreach (IObservation obs in obsProviders)
            inputDim += obs.Size;

        masterPolicy = new NeuralNetworkPolicy(inputDim, outputDim, hiddenDim);
        paramCount = masterPolicy.ParamCount;
        masterTheta = new float[paramCount];

        foreach (ESAgent agent in allAgents)
        {
            agent.ConfigureAgent(
                new NeuralNetworkPolicy(inputDim, outputDim, hiddenDim),
                CloneObs(),
                CloneRews()
            );
        }

        Time.timeScale = trainingTimeScale;
    }

    IEnumerator TrainingLoop()
    {
        isRunning = true;
        while (isRunning)
        {
            SpawnPopulation(popSize);
            yield return EvaluatePopulation(); // waits until everyone Done
            UpdateMaster();
            Cleanup();
            generation++;
            OnGenerationFinished?.Invoke(generation);
        }
    }

    void FixedUpdate()
    {
        if (!isRunning) return;

        float dt = Time.fixedDeltaTime;
        foreach (var ag in population)
            ag.Step(dt);
    }

    void SpawnPopulation(int n)
    {
        population.Clear();
        noiseBank.Clear();

        for (int i = 0; i < n / 2; i++)
        {
            float[] epsilon = new float[paramCount];
            float[] negEpsilon = new float[paramCount];
            for (int k = 0; k < paramCount; k++)
            {
                epsilon[k] = Random.Range(-1f, 1f);
                negEpsilon[k] = -epsilon[k];
            }

            float[] theta_plus = new float[paramCount];
            float[] theta_minus = new float[paramCount];
            for (int k = 0; k < paramCount; k++)
            {
                theta_plus[k] = masterTheta[k] + sigma * epsilon[k];
                theta_minus[k] = masterTheta[k] + sigma * negEpsilon[k];
            }

            CreateAgent(theta_plus, epsilon);

            CreateAgent(theta_minus, negEpsilon);
        }
    }

    void CreateAgent(float[] theta, float[] noiseStored)
    {
        ESAgent ag = GetAgentFromPool();

        ag.PrepareForRun(theta);

        population.Add(ag);
        noiseBank.Add(noiseStored);
    }

    IEnumerator EvaluatePopulation()
    {
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
        int n = population.Count;
        float[] rewards = new float[n];
        for (int i = 0; i < n; i++)
            rewards[i] = population[i].CumulativeReward;

        // Rank normalization to reduce outliers
        int[] ranks = GetRanks(rewards);
        float meanRank = (n - 1) / 2f;
        float stdRank = Mathf.Sqrt((n * n - 1) / 12f); // equation for variance

        float[] grad = new float[paramCount];
        for (int i = 0; i < n; i++)
        {
            float normR = (ranks[i] - meanRank) / stdRank;
            float[] epsilon = noiseBank[i];
            for (int k = 0; k < paramCount; k++)
                grad[k] += normR * epsilon[k];
        }
        for (int k = 0; k < paramCount; k++)
            masterTheta[k] += (alpha / (n * sigma)) * grad[k];
    }

    void Cleanup()
    {
        foreach (ESAgent ag in population)
            ReturnToPool(ag);

        noiseBank.Clear();
        population.Clear();
    }

    ESAgent GetAgentFromPool()
    {
        ESAgent ag = availableAgents.Dequeue();
        ag.Respawn();
        return ag;
    }

    void ReturnToPool(ESAgent ag)
    {
        ag.Despawn();
        availableAgents.Enqueue(ag);
    }

    int[] GetRanks(float[] rewards)
    {
        int n = rewards.Length;
        int[] ranks = new int[n];
        var sorted = new List<(float reward, int idx)>();
        for (int i = 0; i < n; i++) sorted.Add((rewards[i], i));
        sorted.Sort((a, b) => a.reward.CompareTo(b.reward));
        for (int rank = 0; rank < n; rank++)
            ranks[sorted[rank].idx] = rank;
        return ranks;
    }

    IObservation[] CloneObs() =>
    obsProviders.ConvertAll(o => o.Clone())
                .ToArray();

    IReward[] CloneRews() =>
        rewProviders.ConvertAll(r => r.Clone())
                    .ToArray();
}