using System.Collections;
using UnityEngine;

public class ESCManager : MonoBehaviour
{
    public static ESCManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject escMenuObject;

    [Header("Buttons")]
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject exitButton;

    private bool leaveOk = false;

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
        escMenuObject.SetActive(false);
        continueButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseESCMenu);
        exitButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ExitToMainMenu);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (PlayerManager.Instance.CurrentState != PlayerState.Playing)
                return;

            if (escMenuObject.activeSelf)
            {
                CloseESCMenu();
            }
            else
            {
                OpenESCMenu();
            }
        }
    }

    void OpenESCMenu()
    {
        escMenuObject.SetActive(true);
        CursorManager.Instance.SetCursor(CursorType.Default);
        GUIManager.Instance.SetFreezeMovement(true);
    }

    public void CloseESCMenu()
    {
        escMenuObject.SetActive(false);
        CursorManager.Instance.SetCursor(CursorType.Hidden);
        GUIManager.Instance.SetFreezeMovement(false);
    }

    void ExitToMainMenu()
    {
        PopupManager.Instance.Open(PopupType.ExitConfirmation, showConfirm: true, onConfirm: () =>
        {
            CoroutineRunner.Instance.StartCoroutine(ExitToMainMenuEnum());
        });
    }

    IEnumerator ExitToMainMenuEnum()
    {
        CloseESCMenu();
        NetworkManager.Instance.Send("{\"type\":\"leaveRoom\"}");
        yield return new WaitUntil(() => leaveOk);
        PlayerManager.Instance.SetState(PlayerState.Lobby);

        if (MapManager.IsAlive)
            MapManager.Instance.ResetMapState();

        SceneTransitionManager.Instance.TransitionTo("sc_Lobby", withFade: true);
        GUIManager.Instance.SetGUIOpen(true);
        PlayerManager.Instance.gameObject.transform.Find("Player").gameObject.transform.position = new Vector3(0, 0, 0);
        GUIManager.Instance.ShowPanel(GUIManager.Instance.lobbyMatchFound, false);
    }

    public void ActionLeaveOk()
    {
        leaveOk = true;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            if (PlayerManager.Instance.CurrentState == PlayerState.Playing && !escMenuObject.activeSelf)
            {
                OpenESCMenu();
            }
        }
    }
}
