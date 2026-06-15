using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Range(1f, 1.2f)] public float hoverScale = 1.06f;
    [Range(0.7f, 1f)] public float pressedScale = 0.95f;
    [Range(1f, 30f)] public float scaleSpeed = 14f;

    public AudioClip hoverSound;
    public AudioClip clickSound;

    private Vector3 baseScale;
    private Coroutine scaleRoutine;
    private bool pointerInside;
    private AudioSource audioSource;

    private void Awake()
    {
        baseScale = transform.localScale;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
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
        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        AnimateTo(baseScale);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateTo(baseScale * pressedScale);
        PlaySound(clickSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateTo(baseScale * (pointerInside ? hoverScale : 1f));
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
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
