using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ConsoleManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject console;
    [SerializeField] private GameObject consoleButton;
    [SerializeField] private TMP_InputField consoleInput;

    [Header("Logs")]
    [SerializeField] private Transform logParent;
    [SerializeField] private GameObject logPrefab;

    private readonly Dictionary<string, (Action<string[]> action, string description)> commands =
        new Dictionary<string, (Action<string[]>, string)>();

    private void Start()
    {
        Setup();

        LogManager.OnLogCreated += HandleNewLog;

        consoleInput.onSubmit.AddListener(HandleInputSubmit);

        RegisterCommands();
    }

    private void OnDestroy()
    {
        LogManager.OnLogCreated -= HandleNewLog;
        consoleInput.onSubmit.RemoveListener(HandleInputSubmit);
    }

    private void HandleNewLog(string message, LogType type)
    {
        if (logPrefab == null || logParent == null) return;

        GameObject newLog = Instantiate(logPrefab, logParent);
        var titleTMP = newLog.transform.Find("ConsoleLogTitle")?.GetComponent<TextMeshProUGUI>();

        if (titleTMP == null) return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        titleTMP.text = $"[{timestamp}] {message}";

        switch (type)
        {
            case LogType.System: titleTMP.color = Color.gray; break;
            case LogType.Gameplay: titleTMP.color = Color.green; break;
            case LogType.SceneManager: titleTMP.color = new Color(0.5f, 0, 0.5f); break;
            case LogType.Bootstrap: titleTMP.color = new Color(0.3f, 0.8f, 0.9f); break;
            case LogType.FadeManager: titleTMP.color = new Color(0.7f, 0.4f, 0.8f); break;
            case LogType.Network: titleTMP.color = new Color(0.31f, 0.63f, 0.72f); break;
            case LogType.Console: titleTMP.color = new Color(0.25f, 0.65f, 1f); break;
            case LogType.Warning: titleTMP.color = Color.yellow; break;
            case LogType.Error: titleTMP.color = Color.red; break;
        }
    }

    private void Setup()
    {
        if (Debug.isDebugBuild || Application.isEditor)
        {
            consoleButton.SetActive(true);
            consoleButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                console.SetActive(!console.activeSelf);

                if (console.activeSelf)
                    consoleInput.ActivateInputField();
            });
        }
        else
        {
            consoleButton.SetActive(false);
            consoleButton.GetComponent<UnityEngine.UI.Button>().onClick.RemoveAllListeners();
        }
    }

    private void HandleInputSubmit(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        if (input.StartsWith("/"))
        {
            ExecuteCommand(input);
        }
        else
        {
            LogManager.Log($"Unknown command or text: {input}", LogType.Console);
        }

        consoleInput.text = "";
        consoleInput.ActivateInputField();
    }

    private void RegisterCommands()
    {
        commands["help"] = ((args) =>
        {
            string available = "Available commands:\n";
            foreach (var cmd in commands)
                available += $"- /{cmd.Key}: {cmd.Value.description}\n";

            LogManager.Log(available.TrimEnd(), LogType.Console);
        }, "Lists all available commands.");

        commands["clear"] = ((args) =>
        {
            foreach (Transform child in logParent)
                Destroy(child.gameObject);

            LogManager.Log("Console cleared.", LogType.Console);
        }, "Clears the console.");

        commands["version"] = ((args) =>
        {
            string buildType = Debug.isDebugBuild ? "Developer Build" : "Release Build";
            LogManager.Log($"Game Version: {Application.version} ({buildType})", LogType.Console);
        }, "Displays the game version.");

        commands["changescene"] = ((args) =>
        {
            if (args.Length < 1)
            {
                LogManager.Log("Usage: /changescene <scene_name>", LogType.Console);
                return;
            }
            string sceneName = args[0];
            SceneTransitionManager.Instance.TransitionTo(sceneName, withFade: true);
        }, "Changes the current scene to the specified one.");

        commands["network"] = ((args) =>
        {
            if (args.Length < 1)
            {
                LogManager.Log("Usage: /network <connect|disconnect|send> [json]", LogType.Console);
                return;
            }

            string subCommand = args[0].ToLower();

            switch (subCommand)
            {
                case "connect":
                    NetworkManager.Instance.ConnectToServer();
                    break;

                case "disconnect":
                    NetworkManager.Instance.Disconnect();
                    break;

                case "send":
                    if (args.Length < 2)
                    {
                        LogManager.Log("Usage: /network send <json>", LogType.Console);
                        return;
                    }

                    if (!NetworkManager.Instance.IsConnected)
                    {
                        LogManager.Log("Not connected to the server. Use /network connect first.", LogType.Warning);
                        return;
                    }

                    string json = string.Join(" ", args[1..]);
                    try
                    {
                        var obj = JsonUtility.FromJson<object>(json);
                        NetworkManager.Instance.Send(json);
                    }
                    catch (Exception e)
                    {
                        LogManager.Log($"Invalid JSON: {e.Message}", LogType.Error);
                    }
                    break;

                default:
                    LogManager.Log("Unknown subcommand. Use connect, disconnect or send.", LogType.Console);
                    break;
            }
        }, "Network commands: connect, disconnect, send <json>");
    }

    private void ExecuteCommand(string input)
    {
        string[] parts = input.Substring(1).Split(' '); 
        string cmdName = parts[0].ToLower();
        string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        if (commands.TryGetValue(cmdName, out var command))
        {
            command.action.Invoke(args);
        }
        else
        {
            LogManager.Log($"Unknown command: {input}", LogType.Console);
        }
    }
}
