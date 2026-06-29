using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MapCameraController : MonoBehaviour
{
    [Header("References")]
    public RectTransform viewport;
    public RectTransform mapContent;
    public MiniMapController miniMapController;

    [Header("Zoom Settings")]
    public float minZoom = 0.5f;
    public float maxZoom = 6.0f;
    public float zoomSpeed = 0.45f;
    public bool fitMapToViewportOnStart = true;

    [Header("Drag Settings")]
    public float dragSpeed = 1.0f;
    public bool lockMovement = false;

    private bool isDragging;
    private Vector2 lastMousePos;
    private float currentZoom = 1.0f;

    void Start()
    {
        if (fitMapToViewportOnStart)
        {
            FitMapToViewport();
        }
    }

    void Update()
    {
        if (lockMovement) return;
        HandleDrag();
        HandleZoom();
    }

    void HandleDrag()
    {
        Vector2 pointerPos = GetPointerPosition();

        if (GetPointerButtonDown())
        {
            isDragging = true;
            lastMousePos = pointerPos;
        }
        else if (GetPointerButtonUp())
        {
            isDragging = false;
        }

        if (!isDragging) return;

        Vector2 delta = pointerPos - lastMousePos;
        lastMousePos = pointerPos;
        delta *= dragSpeed;

        Vector2 newPos = mapContent.anchoredPosition + delta;
        mapContent.anchoredPosition = ClampPosition(newPos);

        if (miniMapController != null)
            miniMapController.UpdateControlScreen();
    }

    void HandleZoom()
    {
        float scroll = GetScrollDelta();
        if (Mathf.Approximately(scroll, 0f)) return;

        float targetZoom = currentZoom + scroll * zoomSpeed;
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);

        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, GetPointerPosition(), null, out mousePos);

        Vector2 offset = mousePos - mapContent.anchoredPosition;
        float ratio = targetZoom / currentZoom;

        mapContent.localScale = Vector3.one * targetZoom;
        mapContent.anchoredPosition = ClampPosition(mousePos - offset * ratio);

        currentZoom = targetZoom;

        if (miniMapController != null)
            miniMapController.UpdateControlScreen();
    }

    public Vector2 ClampPosition(Vector2 pos)
    {
        Vector2 mapHalf = mapContent.sizeDelta * 0.5f * currentZoom;
        Vector2 vpHalf = viewport.rect.size * 0.5f;

        float minX = vpHalf.x - mapHalf.x;
        float maxX = mapHalf.x - vpHalf.x;
        float minY = vpHalf.y - mapHalf.y;
        float maxY = mapHalf.y - vpHalf.y;

        if (minX > maxX)
        {
            pos.x = 0f;
        }
        else
        {
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
        }

        if (minY > maxY)
        {
            pos.y = 0f;
        }
        else
        {
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
        }

        return pos;
    }

    public void FitMapToViewport()
    {
        if (viewport == null || mapContent == null) return;

        Canvas.ForceUpdateCanvases();

        Vector2 viewportSize = viewport.rect.size;
        Vector2 mapSize = mapContent.sizeDelta;
        if (viewportSize.x <= 0f || viewportSize.y <= 0f || mapSize.x <= 0f || mapSize.y <= 0f) return;

        float coverZoom = Mathf.Max(viewportSize.x / mapSize.x, viewportSize.y / mapSize.y);
        minZoom = Mathf.Max(minZoom, coverZoom);
        maxZoom = Mathf.Max(maxZoom, minZoom + 0.5f);
        currentZoom = Mathf.Clamp(Mathf.Max(currentZoom, minZoom), minZoom, maxZoom);

        mapContent.localScale = Vector3.one * currentZoom;
        mapContent.anchoredPosition = ClampPosition(Vector2.zero);

        if (miniMapController != null)
            miniMapController.UpdateControlScreen();
    }

    public void FocusOnPosition(Vector2 localPos, float targetZoom)
    {
        currentZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        mapContent.localScale = Vector3.one * currentZoom;
        
        Vector2 targetPos = -localPos * currentZoom;
        mapContent.anchoredPosition = ClampPosition(targetPos);

        if (miniMapController != null)
            miniMapController.UpdateControlScreen();
    }

    public float CurrentZoom => currentZoom;

    Vector2 GetPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    bool GetPointerButtonDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.leftButton.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    bool GetPointerButtonUp()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.leftButton.wasReleasedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonUp(0);
#else
        return false;
#endif
    }

    float GetScrollDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            return Mathf.Abs(scroll) > 1f ? scroll / 120f : scroll;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mouseScrollDelta.y;
#else
        return 0f;
#endif
    }
}
