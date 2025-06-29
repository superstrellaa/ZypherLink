using NativeWebSocket;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    public TMP_InputField ipInput;
    public Button connectButton;
    public TMP_Text statusText;

    private WebSocket socket;

    void Start()
    {
        connectButton.onClick.AddListener(ConnectToServer);
    }

    async void ConnectToServer()
    {
        string ip = ipInput.text;
        if (string.IsNullOrEmpty(ip)) return;

        socket = new WebSocket($"ws://{ip}:3000");

        socket.OnOpen += () =>
        {
            statusText.text = "Connected!";
        };

        socket.OnError += (e) =>
        {
            statusText.text = "Error: " + e;
        };

        socket.OnClose += (e) =>
        {
            statusText.text = "Disconnected";
        };

        socket.OnMessage += (bytes) =>
        {
            string msg = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Received: " + msg);
        };

        await socket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        socket?.DispatchMessageQueue();
#endif
    }

    private async void OnApplicationQuit()
    {
        if (socket != null)
            await socket.Close();
    }
}
