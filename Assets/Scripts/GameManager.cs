using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs & Refs")]
    [SerializeField] AgentController agentPrefab;
    [SerializeField] EnvInstance environment;
    [SerializeField] Slider agentCountSlider;

    public A2CLearner learner;
    public EnvInstance env => environment;

    List<AgentController> agents = new();

    void Start()
    {
        learner = new A2CLearner(obsDim: 4, actDim: 2);
        agentCountSlider.onValueChanged.AddListener(OnSliderChanged);
        SpawnAgents((int)agentCountSlider.value);
    }

    void FixedUpdate()
    {
        learner.Learn();        // one gradient step per physics tick
    }

    void OnSliderChanged(float v)
    {
        int desired = (int)v;
        SpawnAgents(desired);
    }

    void SpawnAgents(int k)
    {
        // remove extra
        while (agents.Count > k)
        {
            Destroy(agents[^1].gameObject);
            agents.RemoveAt(agents.Count - 1);
        }
        // add new
        while (agents.Count < k)
        {
            var a = Instantiate(agentPrefab, transform);
            a.Init(this);
            environment.ResetEnv(a);
            agents.Add(a);
            a.rb.WakeUp();
        }
    }
}
