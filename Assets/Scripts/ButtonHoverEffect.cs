using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button))]
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Animation")]
    public float hoverScale = 1.07f;
    public float pressScale = 0.95f;
    public float animSpeed = 8f;

    [Header("Glow / Color")]
    public Outline outline;
    public Color normalOutline   = new Color(0.0f, 0.85f, 1.0f, 0.7f);
    public Color hoverOutline    = new Color(1.0f, 0.55f, 0.0f, 1.0f);
    public Color pressOutline    = new Color(1.0f, 0.75f, 0.1f, 1.0f);

    private Vector3 _targetScale;
    private Vector3 _baseScale;
    private Color   _targetOutline;
    private Coroutine _wobble;

    void Awake()
    {
        _baseScale     = transform.localScale;
        _targetScale   = _baseScale;
        _targetOutline = normalOutline;
        if (outline == null) outline = GetComponent<Outline>();
        if (outline != null) outline.effectColor = normalOutline;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * animSpeed);
        if (outline != null)
            outline.effectColor = Color.Lerp(outline.effectColor, _targetOutline, Time.unscaledDeltaTime * animSpeed);
    }

    public void OnPointerEnter(PointerEventData _)
    {
        _targetScale   = _baseScale * hoverScale;
        _targetOutline = hoverOutline;
        if (_wobble != null) StopCoroutine(_wobble);
        _wobble = StartCoroutine(WobbleRoutine());
    }

    public void OnPointerExit(PointerEventData _)
    {
        _targetScale   = _baseScale;
        _targetOutline = normalOutline;
        if (_wobble != null) { StopCoroutine(_wobble); _wobble = null; }
    }

    public void OnPointerDown(PointerEventData _)
    {
        _targetScale   = _baseScale * pressScale;
        _targetOutline = pressOutline;
    }

    public void OnPointerUp(PointerEventData _)
    {
        _targetScale   = _baseScale * hoverScale;
        _targetOutline = hoverOutline;
    }

    // Quick elastic wobble on enter
    private IEnumerator WobbleRoutine()
    {
        float t = 0f;
        Vector3 peak = _baseScale * (hoverScale + 0.05f);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 6f;
            float s = Mathf.Sin(t * Mathf.PI);
            transform.localScale = Vector3.Lerp(_baseScale * hoverScale, peak, s * (1f - t));
            yield return null;
        }
        _wobble = null;
    }
}
