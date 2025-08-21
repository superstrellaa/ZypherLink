using System.Collections;
using TMPro;
using UnityEngine;

public class GameLoader : MonoBehaviour
{
    private GameObject gameLoaderPanel;
    private TMP_Text statusText;

    void Start()
    {
        gameLoaderPanel = GUIManager.Instance.gameLoaderGUI;
        statusText = GUIManager.Instance.gameLoaderGUI.GetComponentInChildren<TMP_Text>();

        GUIManager.Instance.ShowPanel(gameLoaderPanel, true);

        StartCoroutine(GameLoaderEnum());
    }

    void OnDestroy()
    {
        if (gameLoaderPanel != null)
        {
            GUIManager.Instance.ShowPanel(gameLoaderPanel, false);
        }
    }

    private IEnumerator GameLoaderEnum()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateStatus("Initializing Game Loader...");
        LogManager.Log("Game Loader initialized.", LogType.System);

        UpdateStatus("Connecting to API...");
        yield return ConnectToAPI();

        UpdateStatus("Connecting to WebSocket...");
        yield return ConnectToWebSocket();

        if (NetworkManager.Instance.HasError)
        {
            UpdateStatus("Failed to connect. Stopping loader.");
            yield break;
        }

        UpdateStatus("Loading Lobby...");
        yield return new WaitForSeconds(0.5f);

        SceneTransitionManager.Instance.TransitionTo("sc_Lobby", withFade: true);
    }

    private IEnumerator ConnectToAPI()
    {
        yield return new WaitForSeconds(1.0f);
        LogManager.Log("API connected. Token received.", LogType.Bootstrap);
    }

    private IEnumerator ConnectToWebSocket()
    {
        NetworkManager.Instance.ConnectToServer();

        float timeout = 5f;
        float elapsed = 0f;

        while (!NetworkManager.Instance.IsConnected && !NetworkManager.Instance.HasError)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                NetworkManager.Instance.Disconnect();
                NetworkManager.Instance.MarkError();
                break;
            }
            yield return null;
        }

        if (NetworkManager.Instance.HasError)
        {
            UpdateStatus("Server Error");
            PopupManager.Instance.Open(PopupType.NotServer);
            yield break;
        }

        LogManager.Log("WebSocket connected.", LogType.Bootstrap);
    }

    private void UpdateStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
            LogManager.Log($"[GameLoader] {text}", LogType.Bootstrap);
        }
    }
}
