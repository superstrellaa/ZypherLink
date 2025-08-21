using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoCompleteEntry : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text label;

    public void SetText(string text)
    {
        if (label != null)
            label.text = text;
    }

    public string GetText()
    {
        return label != null ? label.text : "";
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
        {
            Color c = background.color;
            c.a = selected ? 1f : 0f;
            background.color = c;
        }
    }
}
