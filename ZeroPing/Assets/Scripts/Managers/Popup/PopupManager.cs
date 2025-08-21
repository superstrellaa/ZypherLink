using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    private Dictionary<PopupType, (string title, string content)> predefinedPopups;

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
    }

    public void Open(string title, string content)
    {
        if (popupPanel != null)
            popupPanel.SetActive(true);

        if (titleText != null)
            titleText.text = title;

        if (contentText != null)
            contentText.text = content;
    }

    public void Open(PopupType type)
    {
        if (predefinedPopups.ContainsKey(type))
        {
            var data = predefinedPopups[type];
            Open(data.title, data.content);
        }
        else
        {
            Debug.LogWarning($"PopupType {type} not found in predefined popups!");
        }
    }

    public void Close()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }
}
