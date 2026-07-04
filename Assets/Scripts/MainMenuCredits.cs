using UnityEngine;
using UnityEngine.UI;

public class MainMenuCredits : MonoBehaviour
{
    public Text creditsText;

    [TextArea(8, 20)]
    public string creditsContent =
        "ĐẠI VIỆT TRỖI DẬY\n\n" +
        "Nhóm phát triển\n" +
        "HuyPG\nKietNT\nVyMT\n\n" +
        "Thiết kế\n" +
        "HuyPG\nKietNT\nVyMT\n\n" +
        "Lập trình\n" +
        "HuyPG\nKietNT\nVyMT\n\n" +
        "Viết cốt truyện\n" +
        "HuyPG\nKietNT\nVyMT\n\n" +
        "Đặc biệt cảm ơn\n" +
        "Cảm ơn bạn đã ủng hộ.";

    private void Awake()
    {
        if (creditsText != null)
        {
            creditsText.text = creditsContent;
        }
    }
}
