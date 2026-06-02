using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Range(1f, 1.2f)] public float hoverScale = 1.06f;
    [Range(0.7f, 1f)] public float pressedScale = 0.95f;
    [Range(1f, 30f)] public float scaleSpeed = 14f;

    private Vector3 baseScale;
    private Coroutine scaleRoutine;
    private bool pointerInside;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
        }
        transform.localScale = baseScale;
        pointerInside = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        AnimateTo(baseScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        AnimateTo(baseScale);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateTo(baseScale * pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateTo(baseScale * (pointerInside ? hoverScale : 1f));
    }

    private void AnimateTo(Vector3 targetScale)
    {
        if (!isActiveAndEnabled) return;

        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
        }
        scaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale)
    {
        while ((transform.localScale - targetScale).sqrMagnitude > 0.0001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
            yield return null;
        }
        transform.localScale = targetScale;
        scaleRoutine = null;
    }
}
