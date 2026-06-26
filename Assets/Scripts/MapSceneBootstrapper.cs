using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class MapSceneBootstrapper : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas canvas;
    public CanvasScaler canvasScaler;

    [Header("Layout")]
    public float topBarHeight = 85f;
    public float bottomBarHeight = 0f;
    public float leftPanelWidthRatio = 0.20f;
    public float rightMiniMapWidth = 450f;
    public Vector2 mapSize = new Vector2(997f, 1577f);

    [Header("Map Objects")]
    public List<MapObjectData> mapObjects = new List<MapObjectData>();

    [System.Serializable]
    public struct MapObjectData
    {
        public string name;
        public string spriteName;
        public Vector2 anchoredPos;
        public Vector2 size;
    }

    private RectTransform topBarRT;
    private RectTransform mainAreaRT;
    private RectTransform leftPanelRT;
    private RectTransform viewportRT;
    private RectTransform mapContentRT;
    private RectTransform rightMiniMapRT;
    private RectTransform miniMapImageRT;
    private RectTransform controlScreenRT;
    private RectTransform bottomBarRT;
    private RectTransform leftPanelToggleRT;

    private MapCameraController mapCamera;
    private MiniMapController miniMapController;
    private Text hideMapButtonLabel;
    private Text hideQuestPanelButtonLabel;
    private bool miniMapVisible = true;
    private bool questPanelVisible = true;

    private Text questPanelTitleText;
    private RectTransform settingsPanelRT;
    private RectTransform hideMapButtonRT;
    private Slider volumeSlider;
    private Text musicStatusText;
    private const string VolumeKey = "GameVolume";

    void Awake()
    {
        rightMiniMapWidth = 360f; // Force override for MiniMap size
        bottomBarHeight = 0f;     // Force override for Bottom Panel

        // Add Thang Long (Castle) at the specified location
        mapObjects.Add(new MapObjectData {
            name = "Thang Long",
            spriteName = "Castle", // Fixed sprite name path
            anchoredPos = new Vector2(50f, 250f), // Adjusted position (up 50, right 10)
            size = new Vector2(100f, 100f)        // Adjusted size for visibility
        });

        // Add a Village House above Thang Long
        mapObjects.Add(new MapObjectData {
            name = "Village",
            spriteName = "VillageHouse",
            anchoredPos = new Vector2(50f, 400f), // 150px above Thang Long
            size = new Vector2(60f, 60f)          // 3/5 of original size
        });

        // Add a Gold House to the left of Thang Long
        mapObjects.Add(new MapObjectData {
            name = "Gold Mine",
            spriteName = "GoldHouse",
            anchoredPos = new Vector2(-150f, 370f), // Moved down by 30px
            size = new Vector2(50f, 50f)            // Slightly smaller than 60f
        });

        BuildCanvas();
        BuildEventSystem();
        BuildMainArea();
        BuildCenterViewport();
        BuildTopBar();
        BuildBottomBar();
        BuildLeftPanel();
        BuildRightMiniMap();
        BuildMapObjects();
        BuildSettingsPanel();
        WireControllers();
    }

    void Start()
    {
        if (GetComponent<SkillManager>() == null)
        {
            gameObject.AddComponent<SkillManager>();
        }
    }

    void BuildCanvas()
    {
        GameObject go = new GameObject("Canvas");
        go.transform.SetParent(transform, false);

        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        canvasScaler = go.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
    }

    void BuildEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();

        if (eventSystem == null)
        {
            GameObject es = new GameObject("EventSystem");
            eventSystem = es.AddComponent<EventSystem>();
        }

        ConfigureEventSystemInput(eventSystem.gameObject);
    }

    void ConfigureEventSystemInput(GameObject eventSystemObject)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        StandaloneInputModule oldInput = eventSystemObject.GetComponent<StandaloneInputModule>();
        if (oldInput != null)
        {
            oldInput.enabled = false;
            Destroy(oldInput);
        }

        if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (eventSystemObject.GetComponent<BaseInputModule>() == null)
        {
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }

    void BuildTopBar()
    {
        GameObject bar = CreatePanel("TopBar", canvas.transform);
        topBarRT = bar.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0f, 1f);
        topBarRT.anchorMax = new Vector2(1f, 1f);
        topBarRT.pivot = new Vector2(0.5f, 1f);
        topBarRT.sizeDelta = new Vector2(0f, topBarHeight);
        topBarRT.anchoredPosition = Vector2.zero;
        topBarRT.offsetMin = new Vector2(-10f, topBarRT.offsetMin.y);
        topBarRT.offsetMax = new Vector2(10f, topBarRT.offsetMax.y);

        Image bg = bar.GetComponent<Image>();
        ApplySprite(bg, "TopPanel", false);
        bg.color = Color.white;
        bg.raycastTarget = false;

        AddResourceIcon(bar.transform, "FoodPanel", "CropCurrency", "Lương thực: 5000", 0f);
        AddResourceIcon(bar.transform, "CoinPanel", "CoinCurrency", "Tiền: 3000", 1f);
        AddResourceIcon(bar.transform, "ArmyPanel", "ArmyCurrency", "Quân: 1200", 2f);

        Text dateText = CreateText(bar.transform, "DateText", "Năm 1285", 22, TextAnchor.MiddleCenter);
        RectTransform dtRT = dateText.rectTransform;
        dtRT.anchorMin = new Vector2(1f, 0.5f);
        dtRT.anchorMax = new Vector2(1f, 0.5f);
        dtRT.pivot = new Vector2(1f, 0.5f);
        dtRT.sizeDelta = new Vector2(200f, 50f);
        dtRT.anchoredPosition = new Vector2(-350f, 0f);

        Image settingBtn = CreateImage(bar.transform, "SettingButton", "SettingButton", new Vector2(60f, 60f));
        RectTransform stRT = settingBtn.rectTransform;
        stRT.anchorMin = new Vector2(1f, 0.5f);
        stRT.anchorMax = new Vector2(1f, 0.5f);
        stRT.pivot = new Vector2(1f, 0.5f);
        stRT.anchoredPosition = new Vector2(-280f, 0f);
        stRT.sizeDelta = new Vector2(60f, 60f);
        Button sBtn = settingBtn.gameObject.AddComponent<Button>();
        sBtn.onClick.AddListener(ToggleSettingsPanel);
    }

    void AddResourceIcon(Transform parent, string name, string iconSpriteName, string text, float order)
    {
        GameObject panel = new GameObject(name);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(200f, 60f);
        rt.anchoredPosition = new Vector2(300f + order * 220f, 0f);

        Image icon = CreateImage(panel.transform, "Icon", iconSpriteName, new Vector2(48f, 48f));
        RectTransform iconRT = icon.rectTransform;
        iconRT.anchorMin = new Vector2(0f, 0.5f);
        iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot = new Vector2(0f, 0.5f);
        iconRT.anchoredPosition = new Vector2(0f, 0f);
        iconRT.sizeDelta = new Vector2(48f, 48f);

        Text txt = CreateText(panel.transform, "Value", text, 22, TextAnchor.MiddleLeft);
        RectTransform txtRT = txt.rectTransform;
        txtRT.anchorMin = new Vector2(0f, 0.5f);
        txtRT.anchorMax = new Vector2(1f, 0.5f);
        txtRT.pivot = new Vector2(0f, 0.5f);
        txtRT.anchoredPosition = new Vector2(55f, 0f);
        txtRT.sizeDelta = new Vector2(-55f, 0f);
    }

    void BuildBottomBar()
    {
        GameObject bar = CreatePanel("BottomBar", canvas.transform);
        bottomBarRT = bar.GetComponent<RectTransform>();
        bottomBarRT.anchorMin = new Vector2(0f, 0f);
        bottomBarRT.anchorMax = new Vector2(1f, 0f);
        bottomBarRT.pivot = new Vector2(0.5f, 0f);
        bottomBarRT.sizeDelta = new Vector2(0f, bottomBarHeight);
        bottomBarRT.anchoredPosition = Vector2.zero;
        bottomBarRT.offsetMin = new Vector2(-10f, bottomBarRT.offsetMin.y);
        bottomBarRT.offsetMax = new Vector2(10f, bottomBarRT.offsetMax.y);

        Image bg = bar.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);
        bg.raycastTarget = false;

        GameObject eventPanel = CreatePanel("EventPanel", bar.transform);
        RectTransform epRT = eventPanel.GetComponent<RectTransform>();
        epRT.anchorMin = new Vector2(0.5f, 0f);
        epRT.anchorMax = new Vector2(0.5f, 0f);
        epRT.pivot = new Vector2(0.5f, 0f);
        epRT.sizeDelta = new Vector2(300f, 70f);
        epRT.anchoredPosition = new Vector2(-160f, 10f);
        Image epBg = eventPanel.GetComponent<Image>();
        ApplySprite(epBg, "ConfirmButton", true);
        epBg.color = Color.white;
        Button eventBtn = eventPanel.AddComponent<Button>();
        eventBtn.onClick.AddListener(() => OnBottomMenuClicked(true));

        Text epTitle = CreateText(eventPanel.transform, "Title", "SỰ KIỆN", 16, TextAnchor.MiddleCenter);
        epTitle.rectTransform.anchorMin = Vector2.zero;
        epTitle.rectTransform.anchorMax = Vector2.one;
        epTitle.rectTransform.sizeDelta = Vector2.zero;
        epTitle.color = new Color(1f, 0.84f, 0.4f);

        GameObject questPanel = CreatePanel("QuestBottomPanel", bar.transform);
        RectTransform qpRT = questPanel.GetComponent<RectTransform>();
        qpRT.anchorMin = new Vector2(0.5f, 0f);
        qpRT.anchorMax = new Vector2(0.5f, 0f);
        qpRT.pivot = new Vector2(0.5f, 0f);
        qpRT.sizeDelta = new Vector2(300f, 70f);
        qpRT.anchoredPosition = new Vector2(160f, 10f);
        Image qpBg = questPanel.GetComponent<Image>();
        ApplySprite(qpBg, "ConfirmButton", true);
        qpBg.color = Color.white;
        Button questBtn = questPanel.AddComponent<Button>();
        questBtn.onClick.AddListener(() => OnBottomMenuClicked(false));

        Text qpTitle = CreateText(questPanel.transform, "Title", "NHIỆM VỤ", 16, TextAnchor.MiddleCenter);
        qpTitle.rectTransform.anchorMin = Vector2.zero;
        qpTitle.rectTransform.anchorMax = Vector2.one;
        qpTitle.rectTransform.sizeDelta = Vector2.zero;
        qpTitle.color = new Color(1f, 0.84f, 0.4f);
    }

    void BuildMainArea()
    {
        GameObject area = CreatePanel("MainArea", canvas.transform);
        mainAreaRT = area.GetComponent<RectTransform>();
        mainAreaRT.anchorMin = Vector2.zero;
        mainAreaRT.anchorMax = Vector2.one;
        mainAreaRT.pivot = new Vector2(0.5f, 0.5f);
        mainAreaRT.offsetMin = Vector2.zero;
        mainAreaRT.offsetMax = Vector2.zero;

        Image bg = area.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);
        bg.raycastTarget = false;
    }

    void BuildLeftPanel()
    {
        GameObject panel = CreatePanel("LeftPanel", canvas.transform);
        leftPanelRT = panel.GetComponent<RectTransform>();
        float leftWidth = 1920f * leftPanelWidthRatio;

        leftPanelRT.anchorMin = new Vector2(0f, 0f);
        leftPanelRT.anchorMax = new Vector2(0f, 1f);
        leftPanelRT.pivot = new Vector2(0f, 0.5f);
        leftPanelRT.offsetMin = new Vector2(8f, 8f);
        leftPanelRT.offsetMax = new Vector2(8f + leftWidth, -topBarHeight - 8f);

        Image img = panel.GetComponent<Image>();
        Sprite questPanelSprite = LoadSprite("QuestPanel");
        if (questPanelSprite != null)
        {
            img.sprite = questPanelSprite;
            img.type = Image.Type.Simple;
        }

        questPanelTitleText = CreateText(panel.transform, "Title", "SỰ KIỆN", 24, TextAnchor.MiddleCenter);
        questPanelTitleText.rectTransform.anchorMin = new Vector2(0f, 0.85f);
        questPanelTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        questPanelTitleText.rectTransform.sizeDelta = Vector2.zero;
        questPanelTitleText.color = new Color(1f, 0.84f, 0.4f);

        BuildQuestPanelToggle();
    }

    void BuildQuestPanelToggle()
    {
        Image toggleButton = CreateImage(canvas.transform, "ToggleQuestPanelButton", "ConfirmButton", new Vector2(140f, 44f));
        leftPanelToggleRT = toggleButton.rectTransform;
        leftPanelToggleRT.anchorMin = new Vector2(0f, 0.5f);
        leftPanelToggleRT.anchorMax = new Vector2(0f, 0.5f);
        leftPanelToggleRT.pivot = new Vector2(0.5f, 0.5f);
        leftPanelToggleRT.sizeDelta = new Vector2(140f, 44f);
        leftPanelToggleRT.anchoredPosition = new Vector2(leftPanelWidthRatio * 1920f + 20f, 0f);
        leftPanelToggleRT.localEulerAngles = new Vector3(0f, 0f, 90f);
        toggleButton.color = Color.white;

        Button button = toggleButton.gameObject.AddComponent<Button>();
        button.onClick.AddListener(ToggleQuestPanel);

        hideQuestPanelButtonLabel = CreateText(toggleButton.transform, "Label", "Ẩn nhiệm vụ", 15, TextAnchor.MiddleCenter);
        hideQuestPanelButtonLabel.rectTransform.anchorMin = Vector2.zero;
        hideQuestPanelButtonLabel.rectTransform.anchorMax = Vector2.one;
        hideQuestPanelButtonLabel.rectTransform.sizeDelta = Vector2.zero;
        hideQuestPanelButtonLabel.color = new Color(0.95f, 0.85f, 0.55f);
    }

    void BuildCenterViewport()
    {
        GameObject viewport = new GameObject("CenterViewport");
        viewportRT = viewport.AddComponent<RectTransform>();
        viewportRT.SetParent(mainAreaRT, false);

        viewportRT.anchorMin = new Vector2(0f, 0f);
        viewportRT.anchorMax = new Vector2(1f, 1f);
        viewportRT.pivot = new Vector2(0.5f, 0.5f);
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = new Vector2(0f, 0f);

        viewport.AddComponent<Mask>();

        Image vpBg = viewport.AddComponent<Image>();
        vpBg.color = new Color(0.05f, 0.03f, 0.02f);
        vpBg.raycastTarget = false;

        GameObject content = new GameObject("MapContent");
        mapContentRT = content.AddComponent<RectTransform>();
        mapContentRT.SetParent(viewportRT, false);
        mapContentRT.anchorMin = new Vector2(0.5f, 0.5f);
        mapContentRT.anchorMax = new Vector2(0.5f, 0.5f);
        mapContentRT.pivot = new Vector2(0.5f, 0.5f);
        mapContentRT.sizeDelta = mapSize;
        mapContentRT.anchoredPosition = Vector2.zero;
        mapContentRT.localScale = Vector3.one;

        Image mapImg = content.AddComponent<Image>();
        Sprite mapSprite = LoadSprite("Map");
        if (mapSprite != null)
        {
            mapImg.sprite = mapSprite;
        }
        mapImg.preserveAspect = true;
        mapImg.raycastTarget = false;
    }

    void BuildRightMiniMap()
    {
        float miniMapHeight = rightMiniMapWidth * (mapSize.y / mapSize.x);

        GameObject container = CreatePanel("RightMiniMap", canvas.transform);
        rightMiniMapRT = container.GetComponent<RectTransform>();
        rightMiniMapRT.anchorMin = new Vector2(1f, 0f);
        rightMiniMapRT.anchorMax = new Vector2(1f, 0f);
        rightMiniMapRT.pivot = new Vector2(1f, 0f);
        rightMiniMapRT.sizeDelta = new Vector2(rightMiniMapWidth + 10f, miniMapHeight + 15f);
        rightMiniMapRT.anchoredPosition = new Vector2(-22f, 8f);

        Image containerImg = container.GetComponent<Image>();
        containerImg.color = new Color(0.12f, 0.08f, 0.05f, 0.92f);

        GameObject miniMapImg = new GameObject("MiniMapImage");
        miniMapImageRT = miniMapImg.AddComponent<RectTransform>();
        miniMapImageRT.SetParent(rightMiniMapRT, false);
        miniMapImageRT.anchorMin = new Vector2(0.5f, 0.5f);
        miniMapImageRT.anchorMax = new Vector2(0.5f, 0.5f);
        miniMapImageRT.pivot = new Vector2(0.5f, 0.5f);
        miniMapImageRT.sizeDelta = new Vector2(rightMiniMapWidth, miniMapHeight);
        miniMapImageRT.anchoredPosition = new Vector2(0f, 2f);

        Image miniImg = miniMapImg.AddComponent<Image>();
        Sprite miniSprite = LoadSprite("MiniMap");
        if (miniSprite != null)
        {
            miniImg.sprite = miniSprite;
        }
        miniImg.preserveAspect = true;

        GameObject controlScreen = new GameObject("MapControlScreen");
        controlScreenRT = controlScreen.AddComponent<RectTransform>();
        controlScreenRT.SetParent(miniMapImageRT, false);
        controlScreenRT.anchorMin = new Vector2(0.5f, 0.5f);
        controlScreenRT.anchorMax = new Vector2(0.5f, 0.5f);
        controlScreenRT.pivot = new Vector2(0.5f, 0.5f);
        controlScreenRT.sizeDelta = new Vector2(50f, 80f);
        controlScreenRT.anchoredPosition = Vector2.zero;

        Image csImg = controlScreen.AddComponent<Image>();
        Sprite csSprite = LoadSprite("MapControlScreen");
        if (csSprite != null)
        {
            csImg.sprite = csSprite;
            csImg.type = Image.Type.Sliced;
        }
        csImg.color = new Color(1f, 1f, 1f, 0.55f);

        Image hideBtn = CreateImage(canvas.transform, "HideMapButton", "ConfirmButton", new Vector2(140f, 44f));
        hideMapButtonRT = hideBtn.rectTransform;
        hideMapButtonRT.anchorMin = new Vector2(1f, 0f);
        hideMapButtonRT.anchorMax = new Vector2(1f, 0f);
        hideMapButtonRT.pivot = new Vector2(0.5f, 0.5f);
        hideMapButtonRT.anchoredPosition = new Vector2(-rightMiniMapWidth - 42f, 8f + (miniMapHeight + 15f) / 2f);
        hideMapButtonRT.localEulerAngles = new Vector3(0f, 0f, -90f);
        hideBtn.color = Color.white;
        Button hideButton = hideBtn.gameObject.AddComponent<Button>();
        hideButton.onClick.AddListener(ToggleMiniMap);

        hideMapButtonLabel = CreateText(hideBtn.transform, "Label", "Ẩn map", 16, TextAnchor.MiddleCenter);
        hideMapButtonLabel.rectTransform.anchorMin = Vector2.zero;
        hideMapButtonLabel.rectTransform.anchorMax = Vector2.one;
        hideMapButtonLabel.rectTransform.sizeDelta = Vector2.zero;
        hideMapButtonLabel.color = new Color(0.95f, 0.85f, 0.55f);
    }

    void BuildMapObjects()
    {
        if (mapObjects.Count == 0) return;

        foreach (var data in mapObjects)
        {
            Image objImg = CreateImage(mapContentRT.transform, data.name, data.spriteName, data.size);
            RectTransform objRT = objImg.rectTransform;
            objRT.anchorMin = new Vector2(0.5f, 0.5f);
            objRT.anchorMax = new Vector2(0.5f, 0.5f);
            objRT.pivot = new Vector2(0.5f, 0.5f);
            objRT.anchoredPosition = data.anchoredPos;
            objRT.sizeDelta = data.size;

            MapObject mo = objImg.gameObject.AddComponent<MapObject>();
            mo.objectName = data.name;
        }
    }

    void WireControllers()
    {
        mapCamera = gameObject.AddComponent<MapCameraController>();
        mapCamera.viewport = viewportRT;
        mapCamera.mapContent = mapContentRT;

        miniMapController = gameObject.AddComponent<MiniMapController>();
        miniMapController.mainViewport = viewportRT;
        miniMapController.mapContent = mapContentRT;
        miniMapController.miniMapImageRect = miniMapImageRT;
        miniMapController.controlScreenRect = controlScreenRT;

        mapCamera.miniMapController = miniMapController;
    }

    void ToggleMiniMap()
    {
        miniMapVisible = !miniMapVisible;

        if (rightMiniMapRT != null)
        {
            rightMiniMapRT.gameObject.SetActive(miniMapVisible);
        }

        if (hideMapButtonRT != null)
        {
            float miniMapHeight = rightMiniMapWidth * (mapSize.y / mapSize.x);
            float shownX = -rightMiniMapWidth - 42f;
            float shownY = 8f + (miniMapHeight + 15f) / 2f;
            hideMapButtonRT.anchoredPosition = miniMapVisible
                ? new Vector2(shownX, shownY)
                : new Vector2(-24f, shownY);
        }

        if (hideMapButtonLabel != null)
        {
            hideMapButtonLabel.text = miniMapVisible ? "Ẩn map" : "Hiện map";
        }
    }

    void ToggleQuestPanel()
    {
        questPanelVisible = !questPanelVisible;

        if (leftPanelRT != null)
        {
            leftPanelRT.gameObject.SetActive(questPanelVisible);
        }

        if (leftPanelToggleRT != null)
        {
            float shownX = leftPanelWidthRatio * 1920f + 20f;
            leftPanelToggleRT.anchoredPosition = questPanelVisible
                ? new Vector2(shownX, 0f)
                : new Vector2(24f, 0f);
        }

        if (hideQuestPanelButtonLabel != null)
        {
            hideQuestPanelButtonLabel.text = questPanelVisible ? "Ẩn nhiệm vụ" : "Hiện nhiệm vụ";
        }
    }

    void OnBottomMenuClicked(bool isEvent)
    {
        if (questPanelTitleText != null)
        {
            questPanelTitleText.text = isEvent ? "SỰ KIỆN" : "NHIỆM VỤ CHÍNH TUYẾN";
        }
    }

    void BuildSettingsPanel()
    {
        GameObject panel = CreatePanel("SettingsPanel", canvas.transform);
        settingsPanelRT = panel.GetComponent<RectTransform>();
        settingsPanelRT.anchorMin = Vector2.zero;
        settingsPanelRT.anchorMax = Vector2.one;
        settingsPanelRT.pivot = new Vector2(0.5f, 0.5f);
        settingsPanelRT.offsetMin = Vector2.zero;
        settingsPanelRT.offsetMax = Vector2.zero;

        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);

        GameObject menuBox = CreatePanel("MenuBox", panel.transform);
        RectTransform mbRT = menuBox.GetComponent<RectTransform>();
        mbRT.anchorMin = new Vector2(0.5f, 0.5f);
        mbRT.anchorMax = new Vector2(0.5f, 0.5f);
        mbRT.pivot = new Vector2(0.5f, 0.5f);
        mbRT.sizeDelta = new Vector2(400f, 500f);
        mbRT.anchoredPosition = Vector2.zero;
        Image mbImg = menuBox.GetComponent<Image>();
        ApplySprite(mbImg, "SettingPanel", true);

        Text titleText = CreateText(menuBox.transform, "Title", "SETTINGS", 32, TextAnchor.MiddleCenter);
        titleText.rectTransform.anchorMin = new Vector2(0f, 0.8f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.sizeDelta = Vector2.zero;

        CreateSettingsButton(menuBox.transform, "SaveButton", "Save", 100f, () => Debug.Log("Save Clicked"));
        CreateSettingsButton(menuBox.transform, "LoadButton", "Load", 30f, () => Debug.Log("Load Clicked"));
        CreateSettingsButton(menuBox.transform, "QuitButton", "Quit", -40f, () => Application.Quit());

        GameObject musicControl = new GameObject("MusicControl");
        RectTransform mcRT = musicControl.AddComponent<RectTransform>();
        mcRT.SetParent(menuBox.transform, false);
        mcRT.anchorMin = new Vector2(0.5f, 0.5f);
        mcRT.anchorMax = new Vector2(0.5f, 0.5f);
        mcRT.pivot = new Vector2(0.5f, 0.5f);
        mcRT.sizeDelta = new Vector2(300f, 50f);
        mcRT.anchoredPosition = new Vector2(0f, -120f);

        musicStatusText = CreateText(musicControl.transform, "MusicStatus", "Music: 100%", 20, TextAnchor.MiddleLeft);
        musicStatusText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        musicStatusText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        musicStatusText.rectTransform.pivot = new Vector2(0f, 0.5f);
        musicStatusText.rectTransform.sizeDelta = new Vector2(140f, 50f);
        musicStatusText.rectTransform.anchoredPosition = Vector2.zero;

        GameObject sliderObj = new GameObject("VolumeSlider");
        RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
        sliderRT.SetParent(musicControl.transform, false);
        sliderRT.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRT.anchorMax = new Vector2(1f, 0.5f);
        sliderRT.pivot = new Vector2(0f, 0.5f);
        sliderRT.sizeDelta = new Vector2(140f, 20f);
        sliderRT.anchoredPosition = new Vector2(10f, 0f);

        Image sliderBg = sliderObj.AddComponent<Image>();
        ApplySprite(sliderBg, "MusicBar", true);
        sliderBg.color = Color.gray;

        GameObject fillArea = new GameObject("Fill Area");
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.SetParent(sliderRT, false);
        fillAreaRT.anchorMin = new Vector2(0f, 0f);
        fillAreaRT.anchorMax = new Vector2(1f, 1f);
        fillAreaRT.sizeDelta = new Vector2(-10f, 0f);

        GameObject fill = new GameObject("Fill");
        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.SetParent(fillAreaRT, false);
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.sizeDelta = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        ApplySprite(fillImg, "MusicBar", true);

        GameObject handleArea = new GameObject("Handle Slide Area");
        RectTransform handleAreaRT = handleArea.AddComponent<RectTransform>();
        handleAreaRT.SetParent(sliderRT, false);
        handleAreaRT.anchorMin = new Vector2(0f, 0f);
        handleAreaRT.anchorMax = new Vector2(1f, 1f);
        handleAreaRT.sizeDelta = new Vector2(-20f, 0f);

        GameObject handle = new GameObject("Handle");
        RectTransform handleRT = handle.AddComponent<RectTransform>();
        handleRT.SetParent(handleAreaRT, false);
        handleRT.anchorMin = new Vector2(0f, 0f);
        handleRT.anchorMax = new Vector2(0f, 1f);
        handleRT.sizeDelta = new Vector2(20f, 0f);
        Image handleImg = handle.AddComponent<Image>();
        ApplySprite(handleImg, "MusicSwitch", false);

        volumeSlider = sliderObj.AddComponent<Slider>();
        volumeSlider.fillRect = fillRT;
        volumeSlider.handleRect = handleRT;
        volumeSlider.targetGraphic = handleImg;
        volumeSlider.direction = Slider.Direction.LeftToRight;

        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1.0f);
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = savedVolume;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        UpdateMusicVisuals(savedVolume);

        Image closeBtnImg = CreateImage(menuBox.transform, "CloseButton", "ConfirmButton", new Vector2(100f, 40f));
        closeBtnImg.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        closeBtnImg.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        closeBtnImg.rectTransform.pivot = new Vector2(0.5f, 0f);
        closeBtnImg.rectTransform.anchoredPosition = new Vector2(0f, 20f);
        closeBtnImg.rectTransform.sizeDelta = new Vector2(100f, 40f);
        Button closeBtn = closeBtnImg.gameObject.AddComponent<Button>();
        closeBtn.onClick.AddListener(ToggleSettingsPanel);

        Text closeText = CreateText(closeBtnImg.transform, "Text", "Đóng", 16, TextAnchor.MiddleCenter);
        closeText.rectTransform.anchorMin = Vector2.zero;
        closeText.rectTransform.anchorMax = Vector2.one;
        closeText.rectTransform.sizeDelta = Vector2.zero;

        settingsPanelRT.gameObject.SetActive(false);
    }

    void CreateSettingsButton(Transform parent, string name, string text, float yPos, UnityEngine.Events.UnityAction action)
    {
        Image btnImg = CreateImage(parent, name, "BlankButton", new Vector2(200f, 50f));
        if (btnImg.sprite == null) ApplySprite(btnImg, "ConfirmButton", true);
        btnImg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        btnImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        btnImg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        btnImg.rectTransform.anchoredPosition = new Vector2(0f, yPos);
        btnImg.rectTransform.sizeDelta = new Vector2(200f, 50f);

        Button btn = btnImg.gameObject.AddComponent<Button>();
        btn.onClick.AddListener(action);

        Text btnText = CreateText(btnImg.transform, "Text", text, 20, TextAnchor.MiddleCenter);
        btnText.rectTransform.anchorMin = Vector2.zero;
        btnText.rectTransform.anchorMax = Vector2.one;
        btnText.rectTransform.sizeDelta = Vector2.zero;
        btnText.color = Color.white;
    }

    void ToggleSettingsPanel()
    {
        if (settingsPanelRT != null)
        {
            settingsPanelRT.gameObject.SetActive(!settingsPanelRT.gameObject.activeSelf);
        }
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
        UpdateMusicVisuals(value);
    }

    private void UpdateMusicVisuals(float volume)
    {
        if (musicStatusText != null)
        {
            if (volume <= 0f)
            {
                musicStatusText.text = "Music OFF";
            }
            else
            {
                musicStatusText.text = $"Music: {Mathf.RoundToInt(volume * 100)}%";
            }
        }
    }

    // ----- Helpers -----

    void ApplySprite(Image image, string spriteName, bool sliced)
    {
        Sprite sprite = LoadSprite(spriteName);
        if (sprite == null) return;

        image.sprite = sprite;
        if (sliced)
        {
            image.type = Image.Type.Sliced;
        }
    }

    GameObject CreatePanel(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>();
        return go;
    }

    Image CreateImage(Transform parent, string name, string spriteName, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        Sprite sp = LoadSprite(spriteName);
        if (sp != null) img.sprite = sp;
        img.preserveAspect = true;
        return img;
    }

    Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text txt = go.AddComponent<Text>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.alignment = anchor;
        txt.color = new Color(0.9f, 0.85f, 0.7f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.font = font;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        return txt;
    }

    Sprite LoadSprite(string name)
    {
        Texture2D tex = Resources.Load<Texture2D>("MapScene/" + name);
        if (tex == null)
        {
            Debug.LogWarning("MapSceneBootstrapper: Could not load texture 'MapScene/" + name + "'");
            return null;
        }

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
}
