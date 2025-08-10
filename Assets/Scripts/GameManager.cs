using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState { Menu, Running, Paused }

    public static GameManager I;

    private GameState state;

    [SerializeField]
    public Algorithm algorithmManager;

    public event Action OnRunStarted, OnRunStopped;

    public int maxAgents = 1000;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        I = this;
        state = GameState.Menu;
        algorithmManager = GameObject.FindGameObjectWithTag("Algorithm").GetComponent<Algorithm>();
    }

    public void StartRun(int populationSize)
    {
        if (state == GameState.Running) return;

        algorithmManager.StartTraining(populationSize);
        state = GameState.Running;
        OnRunStarted?.Invoke();
    }

    public void StopRun()
    {
        if (state != GameState.Running) return;

        algorithmManager.StopTraining();
        state = GameState.Menu;
        OnRunStopped?.Invoke();
    }
}
