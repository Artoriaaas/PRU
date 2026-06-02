using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    public Texture2D cursorTexture;
    public Vector2 hotspot = Vector2.zero;
    public CursorMode cursorMode = CursorMode.Auto;

    private void Start()
    {
        if (cursorTexture == null)
        {
            Debug.LogWarning("CustomCursor: cursorTexture is not assigned, using default cursor.");
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
            return;
        }

        Cursor.SetCursor(cursorTexture, hotspot, cursorMode);
    }

    private void OnDisable()
    {
        Cursor.SetCursor(null, Vector2.zero, cursorMode);
    }
}
