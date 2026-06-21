using UnityEngine;

public class MiniMapController : MonoBehaviour
{
    [Header("References")]
    public RectTransform mainViewport;
    public RectTransform mapContent;
    public RectTransform miniMapImageRect;
    public RectTransform controlScreenRect;

    void Start()
    {
        UpdateControlScreen();
    }

    public void UpdateControlScreen()
    {
        if (mainViewport == null || mapContent == null ||
            miniMapImageRect == null || controlScreenRect == null)
            return;

        Vector2 viewportSize = mainViewport.rect.size;
        Vector2 mapSize = mapContent.sizeDelta;
        Vector2 miniMapSize = miniMapImageRect.rect.size;
        float zoom = mapContent.localScale.x;

        Vector2 scale = new Vector2(
            miniMapSize.x / mapSize.x,
            miniMapSize.y / mapSize.y
        );

        Vector2 controlSize = new Vector2(
            viewportSize.x / zoom * scale.x,
            viewportSize.y / zoom * scale.y
        );
        controlScreenRect.sizeDelta = controlSize;

        Vector2 controlCenter = new Vector2(
            -mapContent.anchoredPosition.x / zoom * scale.x,
            -mapContent.anchoredPosition.y / zoom * scale.y
        );
        controlScreenRect.anchoredPosition = ClampControlPosition(controlCenter, controlSize, miniMapSize);
    }

    Vector2 ClampControlPosition(Vector2 center, Vector2 size, Vector2 parentSize)
    {
        float halfW = parentSize.x * 0.5f;
        float halfH = parentSize.y * 0.5f;
        float halfCW = size.x * 0.5f;
        float halfCH = size.y * 0.5f;

        float minX = -halfW + halfCW;
        float maxX = halfW - halfCW;
        float minY = -halfH + halfCH;
        float maxY = halfH - halfCH;

        if (minX > maxX)
        {
            center.x = 0f;
        }
        else
        {
            center.x = Mathf.Clamp(center.x, minX, maxX);
        }

        if (minY > maxY)
        {
            center.y = 0f;
        }
        else
        {
            center.y = Mathf.Clamp(center.y, minY, maxY);
        }

        return center;
    }
}
