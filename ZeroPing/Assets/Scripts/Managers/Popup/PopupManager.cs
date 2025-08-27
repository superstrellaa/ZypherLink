using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public enum PopupType
{
    ConnectionRefused,
    RoomDeleted,
    NotServer
}

public class PopupManager : Singleton<PopupManager>
{
    [Header("Popup References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button retryButton;

    private Dictionary<PopupType, (string title, string content)> predefinedPopups;

    private Action retryAction;

    protected override void Awake()
    {
        base.Awake();

        predefinedPopups = new Dictionary<PopupType, (string, string)>
        {
            { PopupType.ConnectionRefused, ("Internet Connection Refused", "You have been disconnected. Please check your connection.") },
            { PopupType.RoomDeleted, ("Room Empty", "The room is empty, you have been kicked out.") },
            { PopupType.NotServer, ("Server unavailable", "Could not connect to the server, try again later.") }
        };
    }

    void Start()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(() =>
            {
                Close();
                retryAction?.Invoke();
            });
        }
    }

    public void Open(string title, string content, bool showRetry = false, Action onRetry = null)
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            GUIManager.Instance.SetGUIOpen(true);
        }

        if (titleText != null)
            titleText.text = title;

        if (contentText != null)
            contentText.text = content;

        retryAction = onRetry;
        if (showRetry)
        {
            retryButton.gameObject.SetActive(true);
            closeButton.gameObject.SetActive(false);
        }
        else
        {
            retryButton.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(true);
        }
    }

    public void Open(PopupType type, bool showRetry = false, Action onRetry = null)
    {
        if (predefinedPopups.ContainsKey(type))
        {
            var data = predefinedPopups[type];
            Open(data.title, data.content, showRetry, onRetry);
        }
        else
        {
            LogManager.LogDebugOnly($"PopupType {type} not found in predefined popups!", LogType.Warning);
        }
    }

    public void Close()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
            CursorManager.Instance.SetCursor(CursorType.Default);
            if (PlayerManager.Instance.CurrentState == PlayerState.Playing)
                GUIManager.Instance.SetGUIOpen(false);
            else
                GUIManager.Instance.SetGUIOpen(true);
        }
    }
}
