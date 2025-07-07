using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Button))]
public class ButtonComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Colors")]
    public Color normalColor = new Color(0.9f, 0.2f, 0.2f);
    public Color hoverColor = Color.white;
    public Color pressedColor = new Color(0.6f, 0.1f, 0.1f);
    public float transitionTime = 0.2f;

    [Header("Text")]
    public TMP_Text buttonText;
    public string displayText = "PLAY";
    public float textTransitionTime = 0.2f;
    public float hoverSpacing = 50f;

    [Header("Scale")]
    public float hoverScale = 1.05f;
    public float scaleTransitionTime = 0.2f;

    [Header("Auto Text Color")]
    public bool autoTextContrast = true;
    public Color lightTextColor = Color.white;
    public Color darkTextColor = Color.black;

    [Header("Events")]
    public UnityEvent onClick;

    private Image background;
    private bool isHovered = false;
    private Coroutine colorRoutine;
    private Coroutine spacingRoutine;
    private Coroutine scaleRoutine;

    private float defaultSpacing = 0f;
    private Vector3 originalScale;

    void Awake()
    {
        background = GetComponent<Image>();
        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>();

        buttonText.text = displayText;
        buttonText.characterSpacing = defaultSpacing;

        background.color = normalColor;
        SetTextContrast(normalColor);

        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        AnimateColor(background.color, hoverColor);
        AnimateTextSpacing(buttonText.characterSpacing, hoverSpacing);
        AnimateScale(transform.localScale, originalScale * hoverScale);
        SetTextContrast(hoverColor);

        CursorManager.Instance?.SetCursor(CursorType.Pointer);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        AnimateColor(background.color, normalColor);
        AnimateTextSpacing(buttonText.characterSpacing, defaultSpacing);
        AnimateScale(transform.localScale, originalScale);
        SetTextContrast(normalColor);

        CursorManager.Instance?.SetCursor(CursorType.Default);
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateColor(background.color, pressedColor);
        SetTextContrast(pressedColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        onClick?.Invoke();
        AnimateColor(background.color, isHovered ? hoverColor : normalColor);
        SetTextContrast(isHovered ? hoverColor : normalColor);
    }

    private void AnimateColor(Color from, Color to)
    {
        if (colorRoutine != null) StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(LerpColor(from, to));
    }

    private IEnumerator LerpColor(Color from, Color to)
    {
        float t = 0f;
        while (t < transitionTime)
        {
            float easedT = EaseOutBack(t / transitionTime);
            background.color = Color.Lerp(from, to, easedT);
            t += Time.deltaTime;
            yield return null;
        }
        background.color = to;
    }

    private void AnimateTextSpacing(float from, float to)
    {
        if (spacingRoutine != null) StopCoroutine(spacingRoutine);
        spacingRoutine = StartCoroutine(LerpSpacing(from, to));
    }

    private IEnumerator LerpSpacing(float from, float to)
    {
        float t = 0f;
        while (t < textTransitionTime)
        {
            float easedT = EaseOutBack(t / textTransitionTime);
            buttonText.characterSpacing = Mathf.Lerp(from, to, easedT);
            t += Time.deltaTime;
            yield return null;
        }
        buttonText.characterSpacing = to;
    }

    private void AnimateScale(Vector3 from, Vector3 to)
    {
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(LerpScale(from, to));
    }

    private IEnumerator LerpScale(Vector3 from, Vector3 to)
    {
        float t = 0f;
        while (t < scaleTransitionTime)
        {
            float easedT = EaseOutBack(t / scaleTransitionTime);
            transform.localScale = Vector3.Lerp(from, to, easedT);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = to;
    }

    private void SetTextContrast(Color bg)
    {
        if (!autoTextContrast || buttonText == null) return;

        float brightness = (bg.r * 0.299f + bg.g * 0.587f + bg.b * 0.114f);
        buttonText.color = (brightness > 0.5f) ? darkTextColor : lightTextColor;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }
}
