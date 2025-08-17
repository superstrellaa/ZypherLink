using UnityEngine;
using UnityEngine.EventSystems;

public class PointerComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Cursor Settings")]
    [SerializeField] private CursorType enterCursor = CursorType.Pointer;
    [SerializeField] private CursorType exitCursor = CursorType.Default;

    public void OnPointerEnter(PointerEventData eventData)
    {
        CursorManager.Instance.SetCursor(enterCursor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CursorManager.Instance.SetCursor(exitCursor);
    }
}
