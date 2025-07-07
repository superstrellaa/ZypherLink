using UnityEngine;

public class LogManager : Singleton<LogManager>
{
#if !UNITY_EDITOR
    private static string logDirectory;
    private static string logFilePath;
#endif

    protected override void Awake()
    {
        base.Awake();
#if !UNITY_EDITOR
        logDirectory = Path.Combine(Application.persistentDataPath, "logs");

        if (!Directory.Exists(logDirectory))
            Directory.CreateDirectory(logDirectory);

        logFilePath = Path.Combine(logDirectory, GetLogFileName());
        Debug.Log("If you want to see all the game logs go to the folder 'logs'");
#endif
    }

    public static void Log(string message, LogType type = LogType.System, bool includeTimestamp = true)
    {
#if UNITY_EDITOR
        string prefix = $"[{type}]";
        string fullMessage = $"{prefix} {message}";

        switch (type)
        {
            case LogType.Warning:
                Debug.LogWarning(fullMessage);
                break;
            case LogType.Error:
                Debug.LogError(fullMessage);
                break;
            default:
                Debug.Log(GetColoredLog(fullMessage, type));
                break;
        }
#else
        string prefix = $"[{type}]";
        string time = includeTimestamp ? $"[{DateTime.Now:HH:mm:ss}]" : "";
        string fullMessage = $"{time} {prefix} {message}";

        SaveLogToFile(fullMessage);
#endif
    }

    public static void LogDebugOnly(string message, LogType type = LogType.System, bool includeTimestamp = true)
    {
        if (!Debug.isDebugBuild && !Application.isEditor)
            return;

        Log(message, type, includeTimestamp);
    }

#if UNITY_EDITOR
    private static string GetColoredLog(string message, LogType type)
    {
        string color = "white";
        switch (type)
        {
            case LogType.System: color = "#808080"; break;
            case LogType.Gameplay: color = "#00FF00"; break;
            case LogType.SceneManager: color = "#800080"; break;
            case LogType.Bootstrap: color = "#4DD0E1"; break;
            case LogType.FadeManager: color = "#BA68C8"; break;
            case LogType.Warning: color = "#FFFF00"; break;
            case LogType.Error: color = "#FF0000"; break;
        }
        return $"<color={color}>{message}</color>";
    }
#endif

#if !UNITY_EDITOR
    private static void SaveLogToFile(string message)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine(message);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving the log file: {ex.Message}");
        }
    }

    private static string GetLogFileName()
    {
        string date = DateTime.Now.ToString("dd-MM-yyyy");

        int index = 1;
        string filePath;

        do
        {
            filePath = Path.Combine(logDirectory, $"{date}.{index}.log");
            index++;
        }
        while (File.Exists(filePath));

        return Path.GetFileName(filePath);
    }
#endif
}
