using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Unity.Barracuda;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class EmbeddedTrainer : MonoBehaviour
{
    [Header("Agents that should receive the live model")]
    public Agent[] controlledAgents;

    [Header("YAML config file (inside StreamingAssets/TrainerConfigs)")]
    public string yamlFileName = "ppo.yaml";

    /* ---------- paths ---------- */
    string BasePath => Path.Combine(Application.streamingAssetsPath, "MLRuntime");
    string PythonExe =>
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        Path.Combine(BasePath, "bin", "python3");
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        Path.Combine(BasePath, "bin", "python3");
#else // Windows
        Path.Combine(BasePath, "python.exe");
#endif
    string LearnScript => Path.Combine(BasePath, "Scripts", "mlagents-learn.exe");
    string YamlPath => Path.Combine(Application.streamingAssetsPath,
                                       "TrainerConfigs", yamlFileName);

    Process trainerProc;
    FileSystemWatcher watcher;
    const int Port = 5004;          // Unity default
    string runId;

    /* ================================================================= */
    /* =======================  PUBLIC UI  ============================= */
    /* ================================================================= */
    public void StartTraining()
    {
        if (trainerProc != null && !trainerProc.HasExited)
        {
            Debug.LogWarning("Trainer already running"); return;
        }

        /* 1) switch every agent to Default so it can talk to Python */
        foreach (var a in controlledAgents)
        {
            var bp = a.GetComponent<BehaviorParameters>();
            if (bp != null) bp.BehaviorType = BehaviorType.Default;
        }

        /* 2) make sure YAML is local to MLRuntime to avoid path issues */
        string localYaml = Path.Combine(BasePath, yamlFileName);
        File.Copy(YamlPath, localYaml, true);

        /* 3) launch trainer */
        runId = "run_" + DateTime.Now.ToString("yyMMdd_HHmmss");
        string args =
            $"{LearnScript} {localYaml} " +
            $"--run-id={runId} --env-args --port={Port} " +
            $"--checkpoint-interval=1 --checkpoint-settings onnx";

        var psi = new ProcessStartInfo(PythonExe, args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = BasePath
        };
        trainerProc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        trainerProc.OutputDataReceived += OnPyLog;
        trainerProc.ErrorDataReceived += OnPyLog;
        trainerProc.Start();
        trainerProc.BeginOutputReadLine();
        trainerProc.BeginErrorReadLine();

        /* 4) watch for new onnx files */
        string resDir = Path.Combine(BasePath, "results", runId);
        Directory.CreateDirectory(resDir);
        watcher = new FileSystemWatcher(resDir, "*.onnx")
        {
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size
        };
        watcher.Created += (_, __) => LoadLatestModel();

        Debug.Log($"[Trainer] started ({runId}) on port {Port}");
    }

    public void StopTraining()
    {
        /* 1) switch back to Heuristic Only */
        foreach (var a in controlledAgents)
        {
            var bp = a.GetComponent<BehaviorParameters>();
            if (bp != null) bp.BehaviorType = BehaviorType.HeuristicOnly;
        }

        /* 2) dispose everything */
        watcher?.Dispose(); watcher = null;

        if (trainerProc != null && !trainerProc.HasExited)
            trainerProc.Kill();
        trainerProc = null;

        Debug.Log("[Trainer] stopped");
    }

    void OnDestroy() => StopTraining();

    /* ================================================================= */
    /* ====================  ONNX hot-swap  ============================ */
    /* ================================================================= */
    void LoadLatestModel()
    {
        try
        {
            string dir = Path.Combine(BasePath, "results", runId);
            var files = Directory.GetFiles(dir, "*.onnx");
            if (files.Length == 0) return;
            Array.Sort(files, StringComparer.Ordinal);
            string newest = files[^1];

            NNModel nn = WrapOnnxIntoNNModel(File.ReadAllBytes(newest));

            foreach (var ag in controlledAgents)
            {
                ag.SetModel("Live", nn);
                ag.EndEpisode(); // restart with new policy
            }
            Debug.Log("[Trainer] swapped " + Path.GetFileName(newest));
        }
        catch (Exception e) { Debug.LogException(e); }
    }

    static readonly FieldInfo fiData =
        typeof(NNModel).GetField("m_ModelData",
                                 BindingFlags.NonPublic | BindingFlags.Instance);

    static NNModel WrapOnnxIntoNNModel(byte[] onnxBytes)
    {
        var nn = ScriptableObject.CreateInstance<NNModel>();
        var data = ScriptableObject.CreateInstance<NNModelData>();
        data.Value = onnxBytes;
        fiData.SetValue(nn, data);
        return nn;
    }

    void OnPyLog(object _, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
            Debug.Log("[PY] " + e.Data);
    }
}