using UnityEngine;
using System;
using System.IO;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FileLogger : MonoBehaviour
{
    private static string logFilePath;
    private static StringBuilder logBuilder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        InitializeLogger();
    }

    private static void InitializeLogger()
    {
        if (logBuilder == null)
        {
            logBuilder = new StringBuilder();
            string fileName = $"GameLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            logFilePath = Path.Combine(Application.persistentDataPath, fileName);
            
            Application.logMessageReceived += HandleLog;
            Debug.Log("File Logger initialized. Log file: " + logFilePath);
        }
    }

    private static void HandleLog(string logString, string stackTrace, LogType type)
    {
        string formattedLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}] {logString}";
        if (type == LogType.Exception)
        {
            formattedLog += "\n" + stackTrace;
        }

        logBuilder.AppendLine(formattedLog);

        // 每次收到日志就写入文件，确保不会因为崩溃而丢失日志
        File.AppendAllText(logFilePath, logBuilder.ToString());
        logBuilder.Clear();
    }

    // 可选：提供一个静态方法来手动写入日志
    public static void Log(string message)
    {
        Debug.Log(message);
    }

    // 编辑器模式下的日志方法
    #if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void InitializeInEditor()
    {
        InitializeLogger();
    }

    public static void EditorLog(string message)
    {
        if (logBuilder == null)
        {
            InitializeLogger();
        }
        HandleLog(message, "", LogType.Log);
    }
    #endif

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }
}
