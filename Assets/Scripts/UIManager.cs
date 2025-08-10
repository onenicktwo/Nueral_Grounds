using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button startBtn;
    [SerializeField] Button stopBtn;
    [Header("Widgets")]
    [SerializeField] Slider popSlider;
    [SerializeField] Text generationText;

    void Start()
    {
        startBtn.onClick.AddListener(OnStartClicked);
        stopBtn.onClick.AddListener(() => GameManager.I.StopRun());

        RegisterCallbacks();
    }

    void RegisterCallbacks()
    {
        GameManager.I.OnRunStarted += HandleRunStarted;
        GameManager.I.OnRunStopped += HandleRunStopped;
        GameManager.I.algorithmManager.OnGenerationFinished += UpdateGenerationText;
    }
    void OnDisable()
    {
        GameManager.I.OnRunStarted -= HandleRunStarted;
        GameManager.I.OnRunStopped -= HandleRunStopped;
        GameManager.I.algorithmManager.OnGenerationFinished -= UpdateGenerationText;
    }

    void OnStartClicked()
    {
        int popSize = Mathf.RoundToInt(popSlider.value);
        GameManager.I.StartRun(popSize);
    }

    void HandleRunStarted() { }
    void HandleRunStopped() { generationText.text = "Gen: 0"; }

    void UpdateGenerationText(int gen) => generationText.text = $"Gen: {gen}";
}
