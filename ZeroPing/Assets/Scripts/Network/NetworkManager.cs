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
    [Header("WebSocket Settings")]
    [SerializeField] private string serverUrl = "ws://localhost:3000";

    private WebSocket socket;

    private float pingInterval = 10f;

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

                if (PlayerManager.Instance.CurrentState != PlayerState.Loading)
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
}
