using UnityEngine;

public class GUIManager : Singleton<GUIManager>
{
    [Header("Lobby GUI")]
    public GameObject lobbyGUI;
    public GameObject lobbyMatchFound;

    [Header("Game Loader GUI")]
    public GameObject gameLoaderGUI;

    public bool IsGUIOpen { get; private set; } = false;

    public bool freezeMovement { get; private set; } = false;

    public void SetGUIOpen(bool value)
    {
        IsGUIOpen = value;
        CursorManager.Instance?.SetCursor(value ? CursorType.Default : CursorType.Hidden);
    }

    public void ShowPanel(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    public void SetFreezeMovement(bool value)
    {
        freezeMovement = value;
    }
}
