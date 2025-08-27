using UnityEngine;
using System.Collections.Generic;

public class CursorManager : Singleton<CursorManager>
{
    [System.Serializable]
    public struct CursorData
    {
        public CursorType type;
        public Texture2D texture;
        public Vector2 hotspot;
    }

    [Header("Cursor Settings")]
    public List<CursorData> cursorDataList;

    private Dictionary<CursorType, CursorData> cursorDict;

    protected override void Awake()
    {
        base.Awake();
        cursorDict = new Dictionary<CursorType, CursorData>();

        foreach (var data in cursorDataList)
        {
            if (!cursorDict.ContainsKey(data.type))
                cursorDict.Add(data.type, data);
        }

        SetCursor(CursorType.Default);
    }

    public void SetCursor(CursorType type)
    {
        if (type == CursorType.Hidden)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            return;
        }

        if (cursorDict.TryGetValue(type, out CursorData data))
        {
            Cursor.SetCursor(data.texture, data.hotspot, CursorMode.Auto);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            LogManager.LogDebugOnly($"[CursorManager] CursorType '{type}' not found in dictionary.", LogType.Warning);
        }
    }
}
