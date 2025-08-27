using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MatchmakingButton : MonoBehaviour
{
    public static MatchmakingButton Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text timerText;
    public Button cancelButton;

    [Header("Linked UI")]
    public GameObject playButtonObject;
    public GameObject matchmakingButtonObject;

    private Coroutine timerCoroutine;
    private float elapsedTime = 0f;
    private bool isSearching = false;

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
        playButtonObject.GetComponent<Button>().onClick.AddListener(StartMatchmaking);
        cancelButton.onClick.AddListener(CancelMatchmaking);
        matchmakingButtonObject.SetActive(false);
    }

    private void StartMatchmaking()
    {
        StartCoroutine(DoStartMatchmaking());
    }

    private IEnumerator DoStartMatchmaking()
    {
        PlayerManager.Instance.SetState(PlayerState.Queue);
        isSearching = true;
        elapsedTime = 0f;

        matchmakingButtonObject.SetActive(true);
        yield return null; 

        playButtonObject.SetActive(false);
        timerCoroutine = StartCoroutine(UpdateTimer());

        LogManager.Log("Matchmaking started", LogType.Network);
        NetworkManager.Instance.Send("{\"type\":\"joinQueue\"}");
    }


    public void CancelMatchmaking()
    {
        PlayerManager.Instance.SetState(PlayerState.Lobby);
        isSearching = false;

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        LogManager.Log("Matchmaking cancelled", LogType.Network);

        if (NetworkManager.Instance.IsConnected)
            NetworkManager.Instance.Send("{\"type\":\"leaveQueue\"}");

        matchmakingButtonObject.SetActive(false);
        playButtonObject.SetActive(true);
    }

    public void RestartMatchmaking()
    {
        isSearching = false;

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        elapsedTime = 0f;

        matchmakingButtonObject.SetActive(false);
        playButtonObject.SetActive(true);
    }

    private IEnumerator UpdateTimer()
    {
        while (isSearching)
        {
            elapsedTime += Time.deltaTime;
            int seconds = Mathf.FloorToInt(elapsedTime);
            int minutes = seconds / 60;
            seconds %= 60;
            timerText.text = $"{minutes:00}:{seconds:00}";
            yield return null;
        }
    }
}
