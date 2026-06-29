using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapObject : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public string objectName;
    private GameObject selectionRing;

    private void Start()
    {
        // Các công trình như Village và Gold Mine không cần vòng tròn
        if (objectName != null && (objectName.Contains("Village") || objectName.Contains("Gold")))
        {
            return;
        }

        // Khởi tạo vòng chọn (Selection Ring) làm con của đối tượng này
        selectionRing = new GameObject("SelectionRing");
        RectTransform rt = selectionRing.AddComponent<RectTransform>();
        rt.SetParent(transform, false);
        
        // Căn chỉnh vòng to hơn thành một chút
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(-15f, -15f); // To hơn 15px mỗi viền
        rt.offsetMax = new Vector2(15f, 15f);

        Image img = selectionRing.AddComponent<Image>();
        img.sprite = LoadRingSprite();
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, 0.85f); // Hơi trong suốt

        // Mặc định ẩn vòng
        selectionRing.SetActive(false);
    }

    private Sprite LoadRingSprite()
    {
        Texture2D tex = Resources.Load<Texture2D>("MapScene/SelectionRing");
        if (tex != null)
        {
            // Tránh lỗi nén không đọc được pixel
            if (!tex.isReadable)
            {
                RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height);
                Graphics.Blit(tex, rt);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                Texture2D readableTex = new Texture2D(tex.width, tex.height);
                readableTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                readableTex.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                tex = readableTex;
            }
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return null;
    }

    private void Update()
    {
        // Nếu vòng đang hiện, cho nó xoay tròn liên tục
        if (selectionRing != null && selectionRing.activeSelf)
        {
            // Tốc độ xoay không quá nhanh (45 độ/giây)
            selectionRing.transform.Rotate(0f, 0f, -45f * Time.deltaTime);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectionRing != null)
        {
            selectionRing.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selectionRing != null)
        {
            selectionRing.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log(objectName + " selected");
        if (objectName != null && (
            objectName.Contains("Thăng Long") ||
            objectName.Contains("Thang Long") ||
            objectName.Contains("Hoan Châu") ||
            objectName.Contains("Trại Yên") ||
            objectName.Contains("Thiên Trường") ||
            objectName.ToLower().Contains("castle")
        ))
        {
            // Tutorial locks: Step < 2 forces Hoan Châu selection
            int tutorialStep = PlayerPrefs.GetInt("TutorialStep", 0);
            if (tutorialStep < 2)
            {
                if (objectName != "Hoan Châu")
                {
                    Debug.Log($"[MapObject] Click blocked. Only 'Hoan Châu' is unlocked during the tutorial. Current step={tutorialStep}");
                    return;
                }
                
                // Set step to 1 (entering Hoan Châu battle)
                PlayerPrefs.SetInt("TutorialStep", 1);
                PlayerPrefs.Save();
            }

            GameManager.activeCastleName = objectName;
            GameManager.levelToLoadName = "Level3";
            UnityEngine.SceneManagement.SceneManager.LoadScene("2D5_Scene");
        }
    }
}
