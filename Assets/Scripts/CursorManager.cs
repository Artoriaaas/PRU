using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tự động set custom cursor cho toàn bộ game ở mọi scene.
/// Không cần gắn vào bất kỳ GameObject nào trong scene.
/// Dùng RuntimeInitializeOnLoadMethod để chạy trước cả khi scene load.
/// </summary>
public static class CursorManager
{
    private static Texture2D _cursorTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        _cursorTexture = Resources.Load<Texture2D>("Cursor");

        if (_cursorTexture != null)
        {
            Cursor.SetCursor(_cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
            Debug.Log("[CursorManager] Custom cursor applied at startup.");
        }
        else
        {
            Debug.LogWarning("[CursorManager] Không tìm thấy Assets/Resources/Cursor.png!");
        }

        // Đảm bảo cursor hiện lại sau mỗi lần chuyển scene
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_cursorTexture != null)
        {
            Cursor.SetCursor(_cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
        }
    }
}
