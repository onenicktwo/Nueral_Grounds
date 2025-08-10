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

    public List<IObservation> obsProviders = new();
    public List<IReward> rewProviders = new();
    private Queue<ESAgent> availableAgents = new();

    IPolicy masterPolicy;
    float[] masterTheta;
    int paramCount;
    readonly List<ESAgent> population = new();
    readonly List<float[]> noiseBank = new();

    int generation;
    bool isRunning;
    Coroutine trainLoop;
    int popSize;

    public event Action<int> OnGenerationFinished;

    void Awake()
    {
        // Testing a fake menu
        DistanceToTarget distanceToTarget = new DistanceToTarget(target);
        obsProviders.Add(distanceToTarget);
        obsProviders.Add(new Velocity());
        Distance dis = new Distance(1f, target, false);
        SphereArea sphereArea = new SphereArea(50f, target, 0.5f, false, true);
        rewProviders.Add(dis);
        rewProviders.Add(sphereArea);

        for (int i = 0; i < GameManager.I.maxAgents; i++)
        {
            GameObject go = Instantiate(agentPrefab, agentParent);
            ESAgent agent = go.GetComponent<ESAgent>();
            ReturnToPool(agent);
        }
    }

    public void StartTraining(int populationSize)
    {
        if (isRunning) return;
        popSize = populationSize;
        if (populationSize % 2 != 0) popSize++;
        Init();
        trainLoop = StartCoroutine(TrainingLoop());
    }

    public void StopTraining()
    {
        if (trainLoop != null) StopCoroutine(trainLoop);
        Cleanup();
        isRunning = false;
        generation = 0;
        masterTheta = new float[paramCount];
        masterPolicy.SetParams(masterTheta);
    }

    void Init()
    {
        foreach (IObservation obsProvider in obsProviders)
            inputDim += obsProvider.Size;

        masterPolicy = new LinearPolicy(inputDim, outputDim); // will be changed for more complex environments
        paramCount = masterPolicy.ParamCount;
        masterTheta = new float[paramCount];
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

            IObservation[] obs1 = CloneObs();
            IReward[] rews1 = CloneRews();
            ESAgent agent1 = GetAgentFromPool();
            agent1.Init(new LinearPolicy(inputDim, outputDim), theta_plus, obs1, rews1);
            foreach (var o in obs1) o.ag = agent1;
            foreach (var r in rews1) r.ag = agent1;
            population.Add(agent1);
            noiseBank.Add(epsilon);

            IObservation[] obs2 = CloneObs();
            IReward[] rews2 = CloneRews();
            ESAgent agent2 = GetAgentFromPool();
            agent2.Init(new LinearPolicy(inputDim, outputDim), theta_minus, obs2, rews2);
            foreach (var o in obs2) o.ag = agent2;
            foreach (var r in rews2) r.ag = agent2;
            population.Add(agent2);
            noiseBank.Add(negEpsilon);
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

        masterPolicy.SetParams(masterTheta);
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