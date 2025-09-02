using NativeWebSocket;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    Loading,
    Lobby,
    Queue,
    Playing
}

[Serializable]
public class PlayerInfo
{
    public string uuid;
    public Vector3 spawnPosition;
    public Quaternion spawnRotation;
}

public class NetworkManager : Singleton<NetworkManager>
{
    public enum EnvironmentType { Development, Production }

    [Header("Environment Selector")]
    [SerializeField] private EnvironmentType environment = EnvironmentType.Development;

    [Header("Development Settings")]
    [SerializeField] private string devServerUrl = "ws://localhost:3000";
    [SerializeField] private string devApiUrl = "http://localhost:3001";

    [Header("Production Settings")]
    [SerializeField] private string prodServerUrl = "ws://31.57.96.123:25631";
    [SerializeField] private string prodApiUrl = "http://31.57.96.123:25607";

    public string serverUrl
    {
        get
        {
            return environment == EnvironmentType.Development ? devServerUrl : prodServerUrl;
        }
    }

    public string apiUrl
    {
        get
        {
            return environment == EnvironmentType.Development ? devApiUrl : prodApiUrl;
        }
    }

    private WebSocket socket;

    public event Action OnSpawnsReady;

    private float pingInterval = 10f;
    private double lastPingSent;
    private List<double> rttSamples = new List<double>();

    public bool IsConnected => socket != null && socket.State == WebSocketState.Open;
    public bool HasError { get; private set; } = false;

    public string MatchId { get; private set; }
    public List<PlayerInfo> Players { get; private set; } = new List<PlayerInfo>();
    public string MapName { get; private set; }
    public bool MatchReady { get; private set; } = false;

    private void Start()
    {
        // ConnectToServer();
    }

    public async void ConnectToServer()
    {
        HasError = false;

        if (IsConnected)
        {
            LogManager.Log("Already connected.", LogType.Network);
            return;
        }

        socket = new WebSocket(serverUrl);

        socket.OnOpen += () =>
        {
            LogManager.Log("WebSocket connected!", LogType.Network);

            InvokeRepeating(nameof(SendPing), pingInterval, pingInterval);
        };

        socket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            LogManager.LogDebugOnly($"[Incoming] {message}", LogType.Network);
            MessageHandler.Handle(message);
        };

        socket.OnError += (err) =>
        {
            LogManager.Log($"WebSocket error: {err}", LogType.Error);
            HasError = true;
        };

        socket.OnClose += (code) =>
        {
            if (this != null && gameObject != null)
                CancelInvoke(nameof(SendPing));

            if (code == WebSocketCloseCode.Normal)
            {
                LogManager.Log("WebSocket closed normally.", LogType.Network);
            }
            else
            {
                LogManager.Log($"WebSocket closed with code {code}", LogType.Warning);
                HasError = true;

                if (PlayerManager.Instance != null && PlayerManager.IsAlive && PlayerManager.Instance.CurrentState != PlayerState.Loading)
                {
                    PlayerManager.Instance.SetState(PlayerState.Loading);
                    SceneTransitionManager.Instance.TransitionTo("sc_LoadingGame", withFade: false);
                }
            }
        };

        await socket.Connect();
    }

    private async void SendPing()
    {
        if (IsConnected)
        {
            lastPingSent = Time.timeAsDouble;
            await socket.SendText("{\"type\":\"ping\"}");
            LogManager.LogDebugOnly("Ping sent to server.", LogType.Network);
        }
    }

    public async void Send(string json)
    {
        if (IsConnected)
        {
            await socket.SendText(json);
            LogManager.LogDebugOnly($"[Sent] {json}", LogType.Network);
        }
        else
        {
            LogManager.Log("Attempted to send but not connected.", LogType.Warning);
        }
    }

    public async void Disconnect()
    {
        if (IsConnected)
        {
            await socket.Close();
            LogManager.Log("WebSocket disconnected.", LogType.Network);
        }
        else
        {
            LogManager.Log("Attempted to disconnect but not connected.", LogType.Warning);
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        socket?.DispatchMessageQueue();
#endif
    }

    protected override async void OnApplicationQuit()
    {
        base.OnApplicationQuit();

        if (socket != null)
            await socket.Close();
    }

    public void MarkError()
    {
        HasError = true;
    }

    public void SetMatchData(string matchId, List<PlayerInfo> players, string mapName)
    {
        MatchId = matchId;
        Players = players;
        MapName = mapName;
        MatchReady = true;

        LogManager.LogDebugOnly($"Match {MatchId} on map '{MapName}' stored. Players: {players.Count}", LogType.Network);
    }

    public void MarkSpawnsReady()
    {
        OnSpawnsReady?.Invoke();
    }

    public double LastPingSent => lastPingSent;

    public void AddRttSample(double rtt)
    {
        rttSamples.Add(rtt);
        if (rttSamples.Count > 50)
            rttSamples.RemoveAt(0);
    }

    public double GetAverageRtt()
    {
        if (rttSamples.Count == 0) return 0;
        double sum = 0;
        foreach (var r in rttSamples) sum += r;
        return sum / rttSamples.Count;
    }

    public double GetJitter()
    {
        if (rttSamples.Count == 0) return 0;
        double avg = GetAverageRtt();
        double variance = 0;
        foreach (var r in rttSamples) variance += Math.Pow(r - avg, 2);
        variance /= rttSamples.Count;
        return Math.Sqrt(variance);
    }

}
