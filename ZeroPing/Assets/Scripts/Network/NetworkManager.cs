using System;
using NativeWebSocket;
using UnityEngine;

public class NetworkManager : Singleton<NetworkManager>
{
    [Header("WebSocket Settings")]
    [SerializeField] private string serverUrl = "ws://localhost:3000";
    [SerializeField] private bool autoConnect = false;

    private WebSocket socket;

    public bool IsConnected => socket != null && socket.State == WebSocketState.Open;

    private void Start()
    {
        if (autoConnect)
            ConnectToServer();
    }

    public async void ConnectToServer()
    {
        if (IsConnected)
        {
            LogManager.Log("Already connected.", LogType.Network);
            return;
        }

        socket = new WebSocket(serverUrl);

        socket.OnOpen += () =>
        {
            LogManager.Log("WebSocket connected!", LogType.Network);
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
        };

        socket.OnClose += (code) =>
        {
            if (code == WebSocketCloseCode.Normal)
            {
                LogManager.Log("WebSocket closed normally.", LogType.Network);
            }
            else
            {
                LogManager.Log($"WebSocket closed with code {code}", LogType.Warning);
            }
        };

        await socket.Connect();
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
}
