using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum ArgType
{
    String,
    Int,
    Float,
    Bool
}

public class CommandArg
{
    public string Name;
    public ArgType Type;
    public bool Optional;

    public CommandArg(string name, ArgType type, bool optional = false)
    {
        Name = name;
        Type = type;
        Optional = optional;
    }
}

public class ConsoleManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject console;
    [SerializeField] private GameObject consoleButton;
    [SerializeField] private TMP_InputField consoleInput;

    [Header("Logs")]
    [SerializeField] private Transform logParent;
    [SerializeField] private GameObject logPrefab;

    [Header("Autocompletion")]
    [SerializeField] private GameObject autoCompletePanel;
    [SerializeField] private GameObject autoCompleteEntryPrefab;
    [SerializeField] private GameObject autoCompleteContainer;

    private List<string> commandHistory = new List<string>();
    private int historyIndex = -1;
    private bool suppressAutoComplete = false;

    private List<AutoCompleteEntry> currentEntries = new List<AutoCompleteEntry>();
    private int selectedIndex = 0;

    private readonly Dictionary<string, (Action<string[]> action, string description, CommandArg[] args)> commands =
    new Dictionary<string, (Action<string[]> action, string description, CommandArg[] args)>();

    private void Start()
    {
        Setup();

        LogManager.OnLogCreated += HandleNewLog;

        consoleInput.onSubmit.AddListener(HandleInputSubmit);
        consoleInput.onValueChanged.AddListener(HandleInputChanged);

        RegisterCommands();
    }

    private void Update()
    {
        HandleSwitchKey();
        HandleCommandHistory();
        HandleArrowKeys();
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
                if (!console.activeSelf)
                {
                    prevGUIState = GUIManager.Instance.IsGUIOpen;

                    console.SetActive(true);
                    GUIManager.Instance.SetGUIOpen(true);
                    consoleInput.ActivateInputField();
                }
                else
                {
                    console.SetActive(false);

                    GUIManager.Instance.SetGUIOpen(prevGUIState && PlayerManager.Instance.CurrentState == PlayerState.Playing);

                    if (!prevGUIState)
                        CursorManager.Instance.SetCursor(CursorType.Hidden);
                }
            });
        }
        else
        {
            consoleButton.SetActive(false);
            consoleButton.GetComponent<UnityEngine.UI.Button>().onClick.RemoveAllListeners();
        }
    }

    private void HandleInputChanged(string currentText)
    {
        if (suppressAutoComplete)
        {
            suppressAutoComplete = false;
            return;
        }

        if (!currentText.StartsWith("/"))
        {
            autoCompletePanel.SetActive(false);
            return;
        }

        string typed = currentText.Length > 1 ? currentText.Substring(1) : "";
        string[] parts = typed.Split(' ');

        if (parts.Length > 2 && parts[0].ToLower() == "network" && parts[1].ToLower() == "send")
        {
            autoCompletePanel.SetActive(false);
            return;
        }

        autoCompletePanel.SetActive(true);

        foreach (Transform child in autoCompleteContainer.transform)
            Destroy(child.gameObject);

        currentEntries.Clear();
        selectedIndex = 0;

        if (parts.Length == 1)
        {
            foreach (var cmd in commands)
            {
                if (string.IsNullOrEmpty(parts[0]) || cmd.Key.StartsWith(parts[0].ToLower()))
                {
                    AddAutoCompleteEntry("/" + cmd.Key);
                }
            }
        }
        else if (parts.Length > 1)
        {
            string cmdName = parts[0].ToLower();
            string argTyped = parts[1].ToLower();

            if (cmdName == "network")
            {
                string[] subCommands = { "connect", "disconnect", "send" };
                foreach (var sub in subCommands)
                {
                    if (sub.StartsWith(argTyped))
                        AddAutoCompleteEntry($"/{cmdName} {sub}");
                }

                if (parts.Length == 2 && parts[1].ToLower() == "send")
                {
                    AddAutoCompleteEntry($"/{cmdName} send [json]");
                }
            }
            else if (cmdName == "openpopup")
            {
                var popupNames = Enum.GetNames(typeof(PopupType));
                foreach (var p in popupNames)
                {
                    if (p.ToLower().StartsWith(argTyped))
                        AddAutoCompleteEntry($"/{cmdName} {p}");
                }

                if (string.IsNullOrEmpty(argTyped))
                    AddAutoCompleteEntry($"/{cmdName} <title> <content>");
            }
        }

        UpdateSelection();

        if (currentEntries.Count == 0)
        {
            autoCompletePanel.SetActive(false);
        }
    }

    private void AddAutoCompleteEntry(string text)
    {
        GameObject entryObj = Instantiate(autoCompleteEntryPrefab, autoCompleteContainer.transform);
        var entry = entryObj.GetComponent<AutoCompleteEntry>();
        if (entry != null)
        {
            entry.SetText(text);
            currentEntries.Add(entry);
        }
    }
    private void UpdateSelection()
    {
        for (int i = 0; i < currentEntries.Count; i++)
        {
            currentEntries[i].SetSelected(i == selectedIndex);
        }
    }

    private bool prevGUIState;

    private void HandleSwitchKey()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            if (!console.activeSelf)
            {
                prevGUIState = GUIManager.Instance.IsGUIOpen;

                console.SetActive(true);
                GUIManager.Instance.SetGUIOpen(true);
                consoleInput.ActivateInputField();
            }
            else
            {
                console.SetActive(false);

                GUIManager.Instance.SetGUIOpen(prevGUIState && PlayerManager.Instance.CurrentState == PlayerState.Playing);
            }
        }
    }

    private void HandleArrowKeys()
    {
        if (!autoCompletePanel.activeSelf || currentEntries.Count == 0) return;

        bool keyPressed = false;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = Mathf.Min(selectedIndex + 1, currentEntries.Count - 1);
            keyPressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = Mathf.Max(selectedIndex - 1, 0);
            keyPressed = true;
        }

        if (keyPressed)
        {
            UpdateSelection();
            consoleInput.DeactivateInputField();
            consoleInput.ActivateInputField();
        }

        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Return))
        {
            if (currentEntries.Count > 0 && currentEntries[selectedIndex] != null)
            {
                consoleInput.text = currentEntries[selectedIndex].GetText();
                consoleInput.caretPosition = consoleInput.text.Length;
                autoCompletePanel.SetActive(false);
            }
        }
    }

    private void HandleCommandHistory()
    {
        if (!consoleInput.isFocused) return;
        if (autoCompletePanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (commandHistory.Count > 0 && historyIndex > 0)
            {
                historyIndex--;
                suppressAutoComplete = true;
                consoleInput.text = commandHistory[historyIndex];
                consoleInput.caretPosition = consoleInput.text.Length;
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (commandHistory.Count > 0 && historyIndex < commandHistory.Count - 1)
            {
                historyIndex++;
                suppressAutoComplete = true;
                consoleInput.text = commandHistory[historyIndex];
                consoleInput.caretPosition = consoleInput.text.Length;
            }
            else if (historyIndex == commandHistory.Count - 1)
            {
                historyIndex++;
                suppressAutoComplete = true;
                consoleInput.text = "";
            }
        }
    }

    private void HandleInputSubmit(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        if (autoCompletePanel.activeSelf && currentEntries.Count > 0)
        {
            consoleInput.text = currentEntries[selectedIndex].GetText();
            consoleInput.caretPosition = consoleInput.text.Length;
            autoCompletePanel.SetActive(false);
            return;
        }

        if (input.StartsWith("/"))
        {
            ExecuteCommand(input);

            if (commandHistory.Count == 0 || commandHistory[^1] != input)
            {
                commandHistory.Add(input);
            }
            historyIndex = commandHistory.Count;
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
        commands.Add("clear", (
            action: args =>
            {
                foreach (Transform child in logParent)
                    Destroy(child.gameObject);

                LogManager.Log("Console cleared.", LogType.Console);
            },
            description: "Clears the console.",
            args: Array.Empty<CommandArg>()
        ));

        commands.Add("version", (
            action: args =>
            {
                string buildType = Debug.isDebugBuild ? "Developer Build" : "Release Build";
                LogManager.Log($"Game Version: {Application.version} ({buildType})", LogType.Console);
            },
            description: "Displays the game version.",
            args: Array.Empty<CommandArg>()
        ));

        commands.Add("changescene", (
            action: args =>
            {
                if (args.Length < 1)
                {
                    LogManager.Log("Usage: /changescene <scene_name>", LogType.Console);
                    return;
                }

                string sceneName = args[0];
                SceneTransitionManager.Instance.TransitionTo(sceneName, withFade: true);
            },
            description: "Changes the current scene to the specified one.",
            args: new CommandArg[]
            {
              new CommandArg("scene_name", ArgType.String)
            }
        ));

        commands.Add("openpopup", (
            action: args =>
            {
                var pm = PopupManager.Instance;
                if (pm == null)
                {
                    LogManager.Log("PopupManager not found in scene.", LogType.Warning);
                    return;
                }
                if (args.Length == 0)
                {
                    LogManager.Log("Usage: /openpopup <PopupType> OR /openpopup <title> <content>", LogType.Console);
                    return;
                }
                if (Enum.TryParse(typeof(PopupType), args[0], true, out var parsed))
                {
                    pm.Open((PopupType)parsed);
                    LogManager.Log($"Opened popup: {args[0]}", LogType.Console);
                    return;
                }
                
                string title = args[0];
                string content = args.Length > 1 ? string.Join(" ", args[1..]) : "";
                pm.Open(title, content);
                LogManager.Log($"Opened custom popup: {title}", LogType.Console);
            },
            description: "Opens a popup. Usage: /openpopup <PopupType> OR /openpopup <title> <content>",
            args: new CommandArg[]
            {
              new CommandArg("popupTypeOrTitle", ArgType.String),
              new CommandArg("content", ArgType.String, optional: true)
            }
        ));

        commands.Add("network", (
            action: args =>
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
                        NetworkManager.Instance.Send(json);
                        break;
                    default:
                        LogManager.Log($"Unknown network command: {subCommand}", LogType.Warning);
                        break;
                }
            },
            description: "Network operations (connect, disconnect, send).",
            args: new CommandArg[]
            {
            new CommandArg("command", ArgType.String),
            new CommandArg("json", ArgType.String, optional: true)
            }
        ));
    }

    private bool TryParseArgs(string[] inputArgs, CommandArg[] expectedArgs, out string[] parsedArgs)
    {
        parsedArgs = new string[expectedArgs.Length];

        int requiredCount = 0;
        foreach (var arg in expectedArgs)
            if (!arg.Optional) requiredCount++;

        if (inputArgs.Length < requiredCount)
        {
            LogManager.Log("Not enough arguments.", LogType.Warning);
            return false;
        }

        for (int i = 0; i < expectedArgs.Length; i++)
        {
            var expected = expectedArgs[i];

            if (i >= inputArgs.Length)
            {
                parsedArgs[i] = null;
                continue;
            }

            string input = inputArgs[i];

            switch (expected.Type)
            {
                case ArgType.Int:
                    if (!int.TryParse(input, out _))
                    {
                        LogManager.Log($"Argument '{expected.Name}' must be an integer.", LogType.Warning);
                        return false;
                    }
                    break;

                case ArgType.Float:
                    if (!float.TryParse(input, out _))
                    {
                        LogManager.Log($"Argument '{expected.Name}' must be a float.", LogType.Warning);
                        return false;
                    }
                    break;

                case ArgType.Bool:
                    if (!bool.TryParse(input, out _))
                    {
                        LogManager.Log($"Argument '{expected.Name}' must be true/false.", LogType.Warning);
                        return false;
                    }
                    break;
            }

            parsedArgs[i] = input;
        }

        return true;
    }

    private void ExecuteCommand(string input)
    {
        string[] parts = input.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string cmdName = parts[0].ToLower();
        string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        if (commands.TryGetValue(cmdName, out var command))
        {
            if (TryParseArgs(args, command.args, out string[] parsedArgs))
            {
                command.action.Invoke(parsedArgs);
            }
        }
        else
        {
            LogManager.Log($"Unknown command: {cmdName}", LogType.Console);
        }
    }
}
