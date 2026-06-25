using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panel Settings")]
    public Sprite panelSprite;
    public Vector2 panelSize = new Vector2(1920f, 300f); // Default height is 1/3 of 218
    public Vector2 panelPosition = new Vector2(0f, 0f);

    [Header("Card Settings")]
    public Vector2 cardSize = new Vector2(80f, 100f);
    public Vector2 cardPosition = new Vector2(0f, 0f);

    [Header("Hint Text Settings")]
    public float hintTextPositionY = 130f;

    private GameObject _canvasObj;
    private Text _unitsText;
    private Button _startBtn;
    private GameObject _gameOverPanel;
    private Text _gameOverText;
    private Text _placementHint;
    
    private GameObject _bottomPanel;
    public GameObject bottomPanel => _bottomPanel;
    private Button _switchViewBtn;
    private Text _switchViewText;
    private Text _scoutingReportText;
    private Button _togglePanelBtn;

    private void Reset()
    {
#if UNITY_EDITOR
        var path = "Assets/Materials/output.png";
        var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
        if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
        {
            importer.textureType = UnityEditor.TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }
        panelSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (panelSprite == null)
        {
            panelSprite = Resources.Load<Sprite>("CardPanel");
        }
#endif
    }

    void Awake()
    {
#if UNITY_EDITOR
        if (panelSprite == null)
        {
            var path = "Assets/Materials/output.png";
            var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
            {
                importer.textureType = UnityEditor.TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
            panelSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
#endif

        if (Instance == null)
        {
            Instance = this;
            
            // Search for existing UI in the scene
            _canvasObj = GameObject.Find("UICanvas");
            
            if (_canvasObj == null)
            {
                CreateUI();
            }
            else
            {
                // Reuse existing canvas
                _bottomPanel = _canvasObj.transform.Find("BottomPanel")?.gameObject;
                if (_bottomPanel != null)
                {
                    Transform cardTrans = _bottomPanel.transform.Find("UnitCard");
                    if (cardTrans != null)
                    {
                        if (cardTrans.GetComponent<DragDropCard>() == null)
                        {
                            cardTrans.gameObject.AddComponent<DragDropCard>();
                        }
                        
                        Transform textTrans = cardTrans.Find("CardText");
                        if (textTrans != null)
                        {
                            Text textComp = textTrans.GetComponent<Text>();
                            if (textComp != null) textComp.raycastTarget = false;
                        }
                    }
                }
                
                _unitsText = _canvasObj.transform.Find("UnitsText")?.GetComponent<Text>();
                _placementHint = _canvasObj.transform.Find("PlacementHint")?.GetComponent<Text>();
                _scoutingReportText = _canvasObj.transform.Find("ScoutingReportText")?.GetComponent<Text>();
                
                Transform startBtnTrans = _canvasObj.transform.Find("StartButton");
                if (startBtnTrans != null)
                {
                    _startBtn = startBtnTrans.GetComponent<Button>();
                    _startBtn.onClick.RemoveAllListeners();
                    _startBtn.onClick.AddListener(() => {
                        GameManager.Instance.StartBattle();
                    });
                    
                    // Setup new visual for StartButton
                    Image startImg = startBtnTrans.GetComponent<Image>();
                    if (startImg != null) {
                        Sprite startSprite = Resources.Load<Sprite>("StartBattle");
                        if (startSprite != null) {
                            startImg.sprite = startSprite;
                            startImg.color = Color.white;
                            startImg.preserveAspect = true;
                            startBtnTrans.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 133f);
                        }
                    }
                    Transform txtTrans = startBtnTrans.Find("Text");
                    if (txtTrans != null) txtTrans.gameObject.SetActive(false);
                }
                
                Transform switchBtnTrans = _canvasObj.transform.Find("SwitchViewButton");
                if (switchBtnTrans != null)
                {
                    _switchViewBtn = switchBtnTrans.GetComponent<Button>();
                    _switchViewText = switchBtnTrans.Find("Text")?.GetComponent<Text>();
                    _switchViewBtn.onClick.RemoveAllListeners();
                    _switchViewBtn.onClick.AddListener(() => {
                        ToggleView();
                    });
                }
                
                Transform toggleBtnTrans = _canvasObj.transform.Find("TogglePanelButton");
                if (toggleBtnTrans == null)
                {
                    Font arial = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (arial == null) arial = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    if (arial == null) arial = Font.CreateDynamicFontFromOSFont("Arial", 24);
                    CreateTogglePanelButton(_canvasObj.transform, arial);
                }
                else
                {
                    _togglePanelBtn = toggleBtnTrans.GetComponent<Button>();
                    Text toggleText = toggleBtnTrans.Find("Text")?.GetComponent<Text>();
                    if (_togglePanelBtn != null)
                    {
                        _togglePanelBtn.onClick.RemoveAllListeners();
                        _togglePanelBtn.onClick.AddListener(() => {
                            if (_bottomPanel != null)
                            {
                                bool isActive = !_bottomPanel.activeSelf;
                                _bottomPanel.SetActive(isActive);
                                if (toggleText != null) toggleText.text = isActive ? "Hide Panel" : "Show Panel";
                            }
                        });
                    }
                }

                if (_canvasObj != null)
                {
                    Transform existingBtn = _canvasObj.transform.Find("HitboxToggleButton");
                    if (existingBtn == null)
                    {
                        CreateHitboxButton(_canvasObj.transform);
                    }
                    else
                    {
                        Button hbBtn = existingBtn.GetComponent<Button>();
                        Text hbText = existingBtn.Find("Text")?.GetComponent<Text>();
                        Image hbImg = existingBtn.GetComponent<Image>();
                        if (hbBtn != null && hbText != null && hbImg != null)
                        {
                            hbBtn.onClick.RemoveAllListeners();
                            hbBtn.onClick.AddListener(() => {
                                ColliderVisualizer.ShowColliders = !ColliderVisualizer.ShowColliders;
                                hbText.text = ColliderVisualizer.ShowColliders ? "Hitbox: ON" : "Hitbox: OFF";
                                hbImg.color = ColliderVisualizer.ShowColliders ? new Color(0.1f, 0.5f, 0.1f, 0.9f) : new Color(0.2f, 0.2f, 0.2f, 0.9f);
                            });
                        }
                    }
                }
                
                Transform gameOverTrans = _canvasObj.transform.Find("GameOverPanel");
                if (gameOverTrans != null)
                {
                    _gameOverPanel = gameOverTrans.gameObject;
                    _gameOverText = gameOverTrans.Find("GameOverText")?.GetComponent<Text>();
                }
            }

            // Ensure EventSystem is present and not duplicated
            GameObject eventSystemObj = GameObject.Find("EventSystem");
            if (eventSystemObj == null)
            {
                eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            UpdatePlacementUI();
        }
        else
        {
            Destroy(this);
        }
    }

    void Update()
    {
        UpdateUIElements();
    }

    public void UpdateUIElements()
    {
        if (_bottomPanel != null)
        {
            RectTransform rtPanel = _bottomPanel.GetComponent<RectTransform>();
            if (rtPanel != null)
            {
                rtPanel.anchoredPosition = panelPosition;
                rtPanel.sizeDelta = new Vector2(1920f, 300f); // Use full 1920x300 size
                
                Image pImg = _bottomPanel.GetComponent<Image>();
                if (pImg != null)
                {
                    Sprite activeSprite = panelSprite != null ? panelSprite : Resources.Load<Sprite>("output");
                    if (activeSprite != null)
                    {
                        pImg.sprite = activeSprite;
                        pImg.color = Color.white;
                        pImg.type = Image.Type.Sliced;
                    }
                    else
                    {
                        pImg.sprite = null;
                        pImg.color = new Color(0, 0, 0, 0.5f);
                    }
                }
            }
        }

        Transform cardTrans = _bottomPanel != null ? _bottomPanel.transform.Find("UnitCard") : null;
        if (cardTrans != null)
        {
            RectTransform rtCard = cardTrans.GetComponent<RectTransform>();
            if (rtCard != null)
            {
                rtCard.anchoredPosition = cardPosition;
                rtCard.sizeDelta = cardSize;
            }
            
            Transform textTrans = cardTrans.Find("CardText");
            if (textTrans != null)
            {
                RectTransform rtText = textTrans.GetComponent<RectTransform>();
                if (rtText != null) rtText.sizeDelta = cardSize;

                Text tComp = textTrans.GetComponent<Text>();
                if (tComp != null) tComp.raycastTarget = false;
            }
        }

        if (_placementHint != null)
        {
            _placementHint.rectTransform.anchoredPosition = new Vector2(0, hintTextPositionY);
        }
    }

    public void CreateUIPreview()
    {
        ClearUIPreview();
        
        if (Instance == null) Instance = this;
        
        CreateUI();
        UpdateUIElements();
    }

    public void ClearUIPreview()
    {
        GameObject canvas = GameObject.Find("UICanvas");
        if (canvas != null) DestroyImmediate(canvas);
        
        GameObject eventSystem = GameObject.Find("EventSystem");
        if (eventSystem != null) DestroyImmediate(eventSystem);
    }

    void CreateUI()
    {
        GameObject eventSystemObj = GameObject.Find("EventSystem");
        if (eventSystemObj == null)
        {
            eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        _canvasObj = new GameObject("UICanvas");
        Canvas canvas = _canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = _canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        _canvasObj.AddComponent<GraphicRaycaster>();

        Font arial = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (arial == null) arial = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (arial == null) arial = Font.CreateDynamicFontFromOSFont("Arial", 24);

        // Units Text
        GameObject textObj = new GameObject("UnitsText");
        textObj.transform.SetParent(_canvasObj.transform, false);
        _unitsText = textObj.AddComponent<Text>();
        _unitsText.font = arial;
        _unitsText.fontSize = 24;
        _unitsText.color = Color.white;
        _unitsText.alignment = TextAnchor.UpperLeft;
        RectTransform rtText = _unitsText.rectTransform;
        rtText.anchorMin = new Vector2(0, 1);
        rtText.anchorMax = new Vector2(0, 1);
        rtText.pivot = new Vector2(0, 1);
        rtText.anchoredPosition = new Vector2(20, -20);
        rtText.sizeDelta = new Vector2(300, 50);

        // Hitbox Toggle Button
        CreateHitboxButton(_canvasObj.transform);

        // Placement Hint
        GameObject hintObj = new GameObject("PlacementHint");
        hintObj.transform.SetParent(_canvasObj.transform, false);
        _placementHint = hintObj.AddComponent<Text>();
        _placementHint.font = arial;
        _placementHint.fontSize = 20;
        _placementHint.color = Color.yellow;
        _placementHint.alignment = TextAnchor.LowerCenter;
        hintObj.SetActive(false);

        // Scouting Report Text
        GameObject reportObj = new GameObject("ScoutingReportText");
        reportObj.transform.SetParent(_canvasObj.transform, false);
        _scoutingReportText = reportObj.AddComponent<Text>();
        _scoutingReportText.font = arial;
        _scoutingReportText.fontSize = 18;
        _scoutingReportText.color = new Color(0.9f, 0.85f, 0.7f);
        _scoutingReportText.alignment = TextAnchor.UpperRight;
        RectTransform rtReport = _scoutingReportText.rectTransform;
        rtReport.anchorMin = new Vector2(1, 1);
        rtReport.anchorMax = new Vector2(1, 1);
        rtReport.pivot = new Vector2(1, 1);
        rtReport.anchoredPosition = new Vector2(-20, -280); // Placed below the skill panel
        rtReport.sizeDelta = new Vector2(360, 100);
        
        // Bottom Panel
        _bottomPanel = new GameObject("BottomPanel");
        _bottomPanel.transform.SetParent(_canvasObj.transform, false);
        Image pImg = _bottomPanel.AddComponent<Image>();
        Sprite activeSprite = panelSprite != null ? panelSprite : Resources.Load<Sprite>("output");
        if (activeSprite != null)
        {
            pImg.sprite = activeSprite;
            pImg.color = Color.white;
            pImg.type = Image.Type.Sliced;
        }
        else
        {
            pImg.color = new Color(0, 0, 0, 0.5f);
        }
        
        RectTransform rtPanel = _bottomPanel.GetComponent<RectTransform>();
        rtPanel.anchorMin = new Vector2(0.5f, 0f);
        rtPanel.anchorMax = new Vector2(0.5f, 0f);
        rtPanel.pivot = new Vector2(0.5f, 0f);
        rtPanel.anchoredPosition = panelPosition;
        rtPanel.sizeDelta = new Vector2(1920f, 300f); // Use full 1920x300 size

        // Unit Card
        GameObject cardObj = new GameObject("UnitCard");
        cardObj.transform.SetParent(_bottomPanel.transform, false);
        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.color = new Color(0.1f, 0.4f, 0.8f, 1f); // Premium blue card
        RectTransform rtCard = cardObj.GetComponent<RectTransform>();
        rtCard.anchorMin = new Vector2(0.5f, 0.5f);
        rtCard.anchorMax = new Vector2(0.5f, 0.5f);
        rtCard.anchoredPosition = cardPosition;
        rtCard.sizeDelta = cardSize;
        
        cardObj.AddComponent<DragDropCard>();

        // Text inside card
        GameObject cardTextObj = new GameObject("CardText");
        cardTextObj.transform.SetParent(cardObj.transform, false);
        Text cardText = cardTextObj.AddComponent<Text>();
        cardText.font = arial;
        cardText.text = "Drag\nMe!";
        cardText.fontSize = 18;
        cardText.color = Color.white;
        cardText.alignment = TextAnchor.MiddleCenter;
        cardText.rectTransform.sizeDelta = cardSize;
        cardText.raycastTarget = false; // Prevent text from blocking interaction

        _placementHint.text = "Drag the blue card onto the grid to place units.";
        RectTransform rtHint = _placementHint.rectTransform;
        rtHint.anchorMin = new Vector2(0.5f, 0);
        rtHint.anchorMax = new Vector2(0.5f, 0);
        rtHint.pivot = new Vector2(0.5f, 0);
        rtHint.anchoredPosition = new Vector2(0, hintTextPositionY);
        rtHint.sizeDelta = new Vector2(800, 50);

        // Start Button
        GameObject btnObj = new GameObject("StartButton");
        btnObj.transform.SetParent(_canvasObj.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        Sprite startSprite = Resources.Load<Sprite>("StartBattle");
        if (startSprite != null) {
            btnImg.sprite = startSprite;
            btnImg.color = Color.white;
            btnImg.preserveAspect = true;
        } else {
            btnImg.color = new Color(0.2f, 0.8f, 0.2f);
        }
        _startBtn = btnObj.AddComponent<Button>();
        
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        Text btnText = btnTextObj.AddComponent<Text>();
        btnText.font = arial;
        btnText.text = "Start Battle";
        btnText.fontSize = 24;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.rectTransform.sizeDelta = new Vector2(160, 50);
        if (startSprite != null) btnTextObj.SetActive(false); // Hide text if we have the image

        RectTransform rtBtn = btnObj.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(1, 0);
        rtBtn.anchorMax = new Vector2(1, 0);
        rtBtn.pivot = new Vector2(1, 0);
        rtBtn.anchoredPosition = new Vector2(-20, 20);
        if (startSprite != null) rtBtn.sizeDelta = new Vector2(200f, 133f);
        else rtBtn.sizeDelta = new Vector2(160, 50);

        _startBtn.onClick.AddListener(() => {
            GameManager.Instance.StartBattle();
        });

        // Switch View Button
        GameObject switchBtnObj = new GameObject("SwitchViewButton");
        switchBtnObj.transform.SetParent(_canvasObj.transform, false);
        Image switchImg = switchBtnObj.AddComponent<Image>();
        switchImg.color = new Color(0.8f, 0.5f, 0.1f);
        _switchViewBtn = switchBtnObj.AddComponent<Button>();
        
        GameObject switchTextObj = new GameObject("Text");
        switchTextObj.transform.SetParent(switchBtnObj.transform, false);
        _switchViewText = switchTextObj.AddComponent<Text>();
        _switchViewText.font = arial;
        _switchViewText.text = "View Enemy >";
        _switchViewText.fontSize = 20;
        _switchViewText.color = Color.white;
        _switchViewText.alignment = TextAnchor.MiddleCenter;
        _switchViewText.rectTransform.sizeDelta = new Vector2(160, 50);

        RectTransform rtSwitch = switchBtnObj.GetComponent<RectTransform>();
        rtSwitch.anchorMin = new Vector2(0, 0);
        rtSwitch.anchorMax = new Vector2(0, 0);
        rtSwitch.pivot = new Vector2(0, 0);
        rtSwitch.anchoredPosition = new Vector2(20, 20);
        rtSwitch.sizeDelta = new Vector2(160, 50);

        _switchViewBtn.onClick.AddListener(() => {
            ToggleView();
        });

        // Toggle Panel Button
        CreateTogglePanelButton(_canvasObj.transform, arial);

        // Game Over Panel
        _gameOverPanel = new GameObject("GameOverPanel");
        _gameOverPanel.transform.SetParent(_canvasObj.transform, false);
        Image panelImg = _gameOverPanel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.7f);
        RectTransform rtGameOverPanel = _gameOverPanel.GetComponent<RectTransform>();
        rtGameOverPanel.anchorMin = Vector2.zero;
        rtGameOverPanel.anchorMax = Vector2.one;
        rtGameOverPanel.sizeDelta = Vector2.zero;

        GameObject goTextObj = new GameObject("GameOverText");
        goTextObj.transform.SetParent(_gameOverPanel.transform, false);
        _gameOverText = goTextObj.AddComponent<Text>();
        _gameOverText.font = arial;
        _gameOverText.fontSize = 60;
        _gameOverText.color = Color.white;
        _gameOverText.alignment = TextAnchor.MiddleCenter;
        RectTransform rtGOText = _gameOverText.rectTransform;
        rtGOText.anchorMin = new Vector2(0.5f, 0.5f);
        rtGOText.anchorMax = new Vector2(0.5f, 0.5f);
        rtGOText.sizeDelta = new Vector2(400, 100);
        
        _gameOverPanel.SetActive(false);
    }

    private void CreateHitboxButton(Transform parent)
    {
        Font arial = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (arial == null) arial = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject hitboxBtnObj = new GameObject("HitboxToggleButton");
        hitboxBtnObj.transform.SetParent(parent, false);
        Image hbImg = hitboxBtnObj.AddComponent<Image>();
        hbImg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f); // Dark background
        
        Outline hbOutline = hitboxBtnObj.AddComponent<Outline>();
        hbOutline.effectColor = new Color(0.95f, 0.85f, 0.55f);
        hbOutline.effectDistance = new Vector2(1f, 1f);

        Button hbBtn = hitboxBtnObj.AddComponent<Button>();
        
        GameObject hbTextObj = new GameObject("Text");
        hbTextObj.transform.SetParent(hitboxBtnObj.transform, false);
        Text hbText = hbTextObj.AddComponent<Text>();
        hbText.font = arial;
        hbText.text = "Hitbox: OFF";
        hbText.fontSize = 15;
        hbText.color = Color.white;
        hbText.alignment = TextAnchor.MiddleCenter;
        hbText.rectTransform.sizeDelta = new Vector2(140, 35);

        RectTransform rtHbBtn = hitboxBtnObj.GetComponent<RectTransform>();
        rtHbBtn.anchorMin = new Vector2(0, 1);
        rtHbBtn.anchorMax = new Vector2(0, 1);
        rtHbBtn.pivot = new Vector2(0, 1);
        rtHbBtn.anchoredPosition = new Vector2(20, -80);
        rtHbBtn.sizeDelta = new Vector2(140, 35);

        hbBtn.onClick.AddListener(() => {
            ColliderVisualizer.ShowColliders = !ColliderVisualizer.ShowColliders;
            hbText.text = ColliderVisualizer.ShowColliders ? "Hitbox: ON" : "Hitbox: OFF";
            hbImg.color = ColliderVisualizer.ShowColliders ? new Color(0.1f, 0.5f, 0.1f, 0.9f) : new Color(0.2f, 0.2f, 0.2f, 0.9f);
        });
    }

    private void CreateTogglePanelButton(Transform parent, Font font)
    {
        GameObject toggleBtnObj = new GameObject("TogglePanelButton");
        toggleBtnObj.transform.SetParent(parent, false);
        Image toggleImg = toggleBtnObj.AddComponent<Image>();
        toggleImg.color = new Color(0.1f, 0.2f, 0.3f, 0.9f);
        _togglePanelBtn = toggleBtnObj.AddComponent<Button>();
        
        GameObject toggleTextObj = new GameObject("Text");
        toggleTextObj.transform.SetParent(toggleBtnObj.transform, false);
        Text toggleText = toggleTextObj.AddComponent<Text>();
        toggleText.font = font;
        toggleText.text = "Hide Panel";
        toggleText.fontSize = 20;
        toggleText.color = Color.white;
        toggleText.alignment = TextAnchor.MiddleCenter;
        toggleText.rectTransform.sizeDelta = new Vector2(160, 40);

        RectTransform rtToggle = toggleBtnObj.GetComponent<RectTransform>();
        rtToggle.anchorMin = new Vector2(0.5f, 0); // Bottom center
        rtToggle.anchorMax = new Vector2(0.5f, 0);
        rtToggle.pivot = new Vector2(0.5f, 0);
        rtToggle.anchoredPosition = new Vector2(0, 320); // Just above the 300px panel
        rtToggle.sizeDelta = new Vector2(160, 40);

        _togglePanelBtn.onClick.AddListener(() => {
            if (_bottomPanel != null)
            {
                bool isActive = !_bottomPanel.activeSelf;
                _bottomPanel.SetActive(isActive);
                toggleText.text = isActive ? "Hide Panel" : "Show Panel";
            }
        });
    }

    private void ToggleView()
    {
        if (CameraController.Instance == null) return;

        if (CameraController.Instance.GetCurrentView() == CameraView.PlayerSetup)
        {
            CameraController.Instance.SetView(CameraView.EnemySetup);
            _switchViewText.text = "< View Player";
        }
        else if (CameraController.Instance.GetCurrentView() == CameraView.EnemySetup)
        {
            CameraController.Instance.SetView(CameraView.PlayerSetup);
            _switchViewText.text = "View Enemy >";
        }

        UpdatePlacementUI();
    }

    public void UpdatePlacementUI()
    {
        if (GameManager.Instance == null) return;

        // Update Scouting Report Text
        var skillManager = Object.FindAnyObjectByType<SkillManager>();
        if (skillManager != null && _scoutingReportText != null)
        {
            _scoutingReportText.text = skillManager.GetScoutingReport();
        }

        bool isPlayer = true;
        if (CameraController.Instance != null && CameraController.Instance.GetCurrentView() == CameraView.EnemySetup)
        {
            isPlayer = false;
        }

        // Dynamically calculate max units based on barracks level
        if (GameManager.Instance != null && skillManager != null)
        {
            GameManager.Instance.maxPlayerUnits = 6 + skillManager.barracksLevel;
        }

        int remaining = isPlayer ? 
            (GameManager.Instance.maxPlayerUnits - GameManager.Instance.placedPlayerUnits) : 0;

        if (_unitsText != null)
        {
            _unitsText.gameObject.SetActive(isPlayer);
            _unitsText.text = $"Available Units: {remaining}";
        }

        if (_bottomPanel != null)
        {
            _bottomPanel.SetActive(isPlayer);

            // Clear old cards to prevent duplicates
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in _bottomPanel.transform)
            {
                if (child.name.StartsWith("UnitCard"))
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            for (int i = 0; i < toDestroy.Count; i++)
            {
                DestroyImmediate(toDestroy[i]);
            }

            if (isPlayer)
            {
                int troopLvl = skillManager != null ? skillManager.troopLevel : 0;
                int numCards = 1 + troopLvl;
                float cardSpacing = 90f;
                float startX = -((numCards - 1) * cardSpacing) / 2f;
                
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                for (int i = 0; i < numCards; i++)
                {
                    int typeIndex = i;
                    GameObject cardObj = new GameObject("UnitCard_" + typeIndex);
                    cardObj.transform.SetParent(_bottomPanel.transform, false);
                    Image cardImg = cardObj.AddComponent<Image>();
                    
                    // Set color and label text based on typeIndex
                    string label = "";
                    if (typeIndex == 0)
                    {
                        cardImg.color = new Color(0.1f, 0.4f, 0.8f, 1f); // Blue
                        label = "Cận Chiến";
                    }
                    else if (typeIndex == 1)
                    {
                        cardImg.color = new Color(0.1f, 0.6f, 0.2f, 1f); // Green
                        label = "Cung Thủ";
                    }
                    else if (typeIndex == 2)
                    {
                        cardImg.color = new Color(0.5f, 0.1f, 0.7f, 1f); // Purple
                        label = "Kỵ Binh";
                    }
                    else if (typeIndex == 3)
                    {
                        cardImg.color = new Color(0.85f, 0.5f, 0.1f, 1f); // Gold/Orange
                        label = "Hổ Bôn";
                    }

                    RectTransform rtCard = cardObj.GetComponent<RectTransform>();
                    rtCard.anchorMin = new Vector2(0.5f, 0.5f);
                    rtCard.anchorMax = new Vector2(0.5f, 0.5f);
                    rtCard.pivot = new Vector2(0.5f, 0.5f);
                    rtCard.anchoredPosition = new Vector2(startX + i * cardSpacing, cardPosition.y);
                    rtCard.sizeDelta = cardSize;

                    var ddc = cardObj.AddComponent<DragDropCard>();
                    ddc.unitTypeIndex = typeIndex;

                    // Text inside card
                    GameObject cardTextObj = new GameObject("CardText");
                    cardTextObj.transform.SetParent(cardObj.transform, false);
                    Text cardText = cardTextObj.AddComponent<Text>();
                    cardText.font = font;
                    cardText.text = label;
                    cardText.fontSize = 14;
                    cardText.color = Color.white;
                    cardText.alignment = TextAnchor.MiddleCenter;
                    cardText.rectTransform.sizeDelta = cardSize;
                    cardText.raycastTarget = false;
                }
            }
        }

        if (_placementHint != null)
        {
            _placementHint.text = isPlayer ? 
                "Drag the card onto the grid to place units." : 
                "Viewing enemy setup (Pre-configured from Level Editor)";
        }

        // Lock or Unlock Switch View Button based on Scouting Level
        if (_switchViewBtn != null)
        {
            if (skillManager != null && skillManager.scoutingLevel < 3)
            {
                _switchViewBtn.interactable = false;
                if (_switchViewText != null)
                {
                    _switchViewText.text = "🔒 View Enemy (Lv3)";
                }
            }
            else
            {
                _switchViewBtn.interactable = true;
                if (_switchViewText != null)
                {
                    _switchViewText.text = (CameraController.Instance != null && CameraController.Instance.GetCurrentView() == CameraView.EnemySetup)
                        ? "< View Player" 
                        : "View Enemy >";
                }
            }
        }
        
        if (_togglePanelBtn != null)
        {
            _togglePanelBtn.gameObject.SetActive(isPlayer);
        }
    }

    public void HidePlacementUI()
    {
        if (_unitsText != null) _unitsText.gameObject.SetActive(false);
        if (_startBtn != null) _startBtn.gameObject.SetActive(false);
        if (_placementHint != null) _placementHint.gameObject.SetActive(false);
        if (_bottomPanel != null) _bottomPanel.SetActive(false);
        if (_switchViewBtn != null) _switchViewBtn.gameObject.SetActive(false);
        if (_togglePanelBtn != null) _togglePanelBtn.gameObject.SetActive(false);
        if (_scoutingReportText != null) _scoutingReportText.gameObject.SetActive(false);
        
        GameObject skillPanel = GameObject.Find("UICanvas/SkillPanel");
        if (skillPanel != null) skillPanel.SetActive(false);

        GameObject hitboxBtn = GameObject.Find("UICanvas/HitboxToggleButton");
        if (hitboxBtn != null) hitboxBtn.SetActive(false);
    }

    public void ShowGameOver(bool playerWon)
    {
        _gameOverPanel.SetActive(true);
        if (playerWon)
        {
            _gameOverText.text = "VICTORY!";
            _gameOverText.color = Color.green;
        }
        else
        {
            _gameOverText.text = "DEFEAT!";
            _gameOverText.color = Color.red;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UIManager))]
public class UIManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        
        DrawDefaultInspector();
        
        UIManager manager = (UIManager)target;

        if (EditorGUI.EndChangeCheck())
        {
            manager.UpdateUIElements();
            if (!Application.isPlaying)
            {
                // If in Edit Mode, we update the active preview
                manager.UpdateUIElements();
            }
        }

        GUILayout.Space(12);

        if (!Application.isPlaying)
        {
            if (GUILayout.Button("▶  Preview UI in Editor", GUILayout.Height(36)))
            {
                manager.CreateUIPreview();
            }

            if (GUILayout.Button("✖  Clear UI Preview", GUILayout.Height(24)))
            {
                manager.ClearUIPreview();
            }
        }
    }
}
#endif
