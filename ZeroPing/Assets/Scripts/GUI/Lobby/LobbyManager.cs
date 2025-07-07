using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    private GameObject lobbyPanel;

    void Start()
    {
        if (lobbyPanel == null)
        {
            lobbyPanel = GUIManager.Instance.lobbyGUI;
        }

        if (lobbyPanel != null)
        {
            GUIManager.Instance.SetGUIOpen(true);
            GUIManager.Instance.ShowPanel(lobbyPanel, true);
        }
        else
        {
            Debug.LogWarning("[LobbyManager] Lobby GUI not found in the scene.");
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
}
