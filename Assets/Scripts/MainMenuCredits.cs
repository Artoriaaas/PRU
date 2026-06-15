using UnityEngine;
using UnityEngine.UI;

public class MainMenuCredits : MonoBehaviour
{
    public Text creditsText;

    [TextArea(8, 20)]
    public string creditsContent =
        "DAI VIET RISING\n\n" +
        "Development Team\n" +
        "HuyPG\nKietNT\nVyMT\n\n" +
        "Design\n" +
        "HuyPG\nKietNT\nVyMT\n\n" +
        "Programming\n" +
        "HuyPG\nKietNT\nVyMT\n\n" +
        "Story Writing\n" +
        "HuyPG\nKietNT\nVyMT\n\n" +
        "Special Thanks\n" +
        "For supporting our project.";

    private void Awake()
    {
        if (creditsText != null)
        {
            creditsText.text = creditsContent;
        }
    }
}
