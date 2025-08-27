using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class GameLoader : MonoBehaviour
{
    private GameObject gameLoaderPanel;
    private TMP_Text statusText;
    private string apiToken;

    void Start()
    {
        gameLoaderPanel = GUIManager.Instance.gameLoaderGUI;
        statusText = GUIManager.Instance.gameLoaderGUI.GetComponentInChildren<TMP_Text>();

        GUIManager.Instance.ShowPanel(gameLoaderPanel, true);
        GUIManager.Instance.SetGUIOpen(true);

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
        PlayerManager.Instance.SetState(PlayerState.Lobby);
    }

    private IEnumerator ConnectToAPI()
    {
        using (UnityWebRequest www = new UnityWebRequest("http://localhost:3001/auth/login", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(new byte[0]);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                LogManager.Log($"API Error: {www.error}", LogType.Error);
                apiToken = null;

                bool retryPressed = false;

                UpdateStatus("API Error");
                PopupManager.Instance.Open(
                    PopupType.NotServer,
                    showRetry: true,
                    onRetry: () =>
                    {
                        UpdateStatus("Retrying API connection...");
                        retryPressed = true;
                    });

                yield return new WaitUntil(() => retryPressed);

                yield return ConnectToAPI();
                yield break;
            }
            else
            {
                var responseJson = www.downloadHandler.text;
                apiToken = JsonUtility.FromJson<TokenResponse>(responseJson).token;
                LogManager.LogDebugOnly("API connected. Token received: " + apiToken, LogType.Bootstrap);
                LogManager.Log("API connected.", LogType.Bootstrap);
                UpdateStatus("Waiting status from API...");
                yield return new WaitForSeconds(0.5f);
            }
        }
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
            bool retryPressed = false;

            UpdateStatus("Server Error");
            PopupManager.Instance.Open(
                PopupType.NotServer,
                showRetry: true,
                onRetry: () =>
                {
                    StartCoroutine(ConnectToWebSocket());
                    UpdateStatus("Retrying WebSocket connection...");
                    retryPressed = true;
                });
            yield return new WaitUntil(() => retryPressed);

            yield return ConnectToWebSocket();
            yield break;
        }

        if (!string.IsNullOrEmpty(apiToken))
        {
            var authMessage = new WebSocketAuthMessage
            {
                type = "auth",
                token = apiToken
            };
            string authJson = JsonUtility.ToJson(authMessage);
            NetworkManager.Instance.Send(authJson);
        }

        UpdateStatus("Waiting status from WebSocket...");
        yield return new WaitForSeconds(0.5f);
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

    [System.Serializable]
    private class TokenResponse
    {
        public string token;
    }

    [System.Serializable]
    private class WebSocketAuthMessage
    {
        public string type;
        public string token;
    }
}
