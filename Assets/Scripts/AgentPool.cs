using System.Collections.Generic;
using UnityEngine;

public class AgentPool : MonoBehaviour
{
    [SerializeField] GameObject agentPrefab;
    readonly Queue<ESAgent> pool = new();

    public ESAgent Get()
    {
        if (pool.Count == 0) Add(8);
        ESAgent ag = pool.Dequeue();
        ag.gameObject.SetActive(true);
        return ag;
    }

    public void Recycle(ESAgent ag)
    {
        ag.gameObject.SetActive(false);
        pool.Enqueue(ag);
    }

    void Add(int n)
    {
        for (int i = 0; i < n; i++)
        {
            var go = Instantiate(agentPrefab, transform);
            go.SetActive(false);
            pool.Enqueue(go.GetComponent<ESAgent>());
        }
    }
}