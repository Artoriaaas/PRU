using UnityEngine;
using UnityEngine.UI;

public class HideDebugButtons : MonoBehaviour
{
    void Start()
    {
        var hitbox = GameObject.Find("HitboxToggleButton");
        if (hitbox != null) hitbox.SetActive(false);

        var levelEditor = GameObject.Find("TogglePanelButton");
        if (levelEditor != null) levelEditor.SetActive(false);

        var startBtn = GameObject.Find("StartButton");
        if (startBtn != null)
        {
            var img = startBtn.GetComponent<Image>();
            if (img != null)
            {
                var sprite = Resources.Load<Sprite>("ChienDau");
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.preserveAspect = true;
                }
            }
        }

        Destroy(gameObject);
    }
}