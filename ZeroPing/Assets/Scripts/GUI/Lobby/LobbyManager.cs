using System.Collections;
using TMPro;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private GameObject lobbyPanel;
    private GameObject lobbyMatchFound;
    private TextMeshProUGUI countdownText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (lobbyPanel == null)
            lobbyPanel = GUIManager.Instance.lobbyGUI;

        if (lobbyMatchFound == null)
            lobbyMatchFound = GUIManager.Instance.lobbyMatchFound;

        if (lobbyPanel != null)
        {
            GUIManager.Instance.SetGUIOpen(true);
            GUIManager.Instance.ShowPanel(lobbyPanel, true);
        }
        else
        {
            Debug.LogWarning("[LobbyManager] Lobby GUI not found in the scene.");
        }

        if (lobbyMatchFound != null)
        {
            countdownText = lobbyMatchFound.transform.Find("MatchFoundCount")?.GetComponent<TextMeshProUGUI>();
            if (countdownText == null)
                Debug.LogWarning("[LobbyManager] MatchFoundCount no encontrado dentro de lobbyMatchFound");
        }
    }

    void OnDestroy()
    {
        if (GUIManager.IsAlive && lobbyPanel != null)
        {
            GUIManager.Instance.SetGUIOpen(false);
            GUIManager.Instance.ShowPanel(lobbyPanel, false);
        }
    }

    public IEnumerator MatchFoundScreenEnum()
    {
        if (!PlayerManager.Instance.CurrentState.Equals(PlayerState.Queue))
        {
            LogManager.Log("Player is not in queue state, aborting match found screen.", LogType.Warning);
            yield break;
        }

        MatchmakingButton.Instance.RestartMatchmaking();

        yield return new WaitForSeconds(0.5f);

        GUIManager.Instance.ShowPanel(lobbyMatchFound, true);

        int countdown = 5;
        while (countdown > 0)
        {
            if (countdownText != null)
                countdownText.text = countdown.ToString();

            yield return new WaitForSeconds(1f);
            countdown--;
        }

        LogManager.Log("Countdown finished, starting game...", LogType.Gameplay);
        SceneTransitionManager.Instance.TransitionTo($"sc_{NetworkManager.Instance.MapName.Replace(" ", "")}", withFade: true);
        PlayerManager.Instance.SetState(PlayerState.Playing);
        countdown = 5;
        yield break;
    }
}
