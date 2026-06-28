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
    private Image _gameOverImage;
    private Text _placementHint;
    
    private GameObject _bottomPanel;
    public GameObject bottomPanel => _bottomPanel;
    private GameObject _panelContent; // child container holding buttons + cards (can be hidden)
    private Button _switchViewBtn;
    private Text _switchViewText;
    private Text _scoutingReportText;
    private Button _togglePanelBtn;
    private GameObject _showArrowBtn; // show.png button shown when panel is collapsed
    private bool _panelCollapsed = false;
    private Image _transitionFog;

    private GameObject _errorBanner;
    private Text _errorBannerText;
    private float _errorBannerTimer = 0f;

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

        // Ensure CustomUI sprites are imported as Sprite
        string[] customSpritePaths = new string[] {
            "Assets/Resources/CustomUI/BoBinhCard.png",
            "Assets/Resources/CustomUI/CungThuCard.png",
            "Assets/Resources/CustomUI/TuongQuanCard.png",
            "Assets/Resources/CustomUI/show.png",
        };
        foreach (var spritePath in customSpritePaths)
        {
            var si = UnityEditor.AssetImporter.GetAtPath(spritePath) as UnityEditor.TextureImporter;
            if (si != null && si.textureType != UnityEditor.TextureImporterType.Sprite)
            {
                si.textureType = UnityEditor.TextureImporterType.Sprite;
                si.SaveAndReimport();
            }
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
                
                Transform startBtnTrans = _bottomPanel?.transform.Find("StartButton") ?? _canvasObj.transform.Find("StartButton");
                if (startBtnTrans != null)
                {
                    if (_bottomPanel != null && !startBtnTrans.IsChildOf(_bottomPanel.transform))
                        startBtnTrans.SetParent(_bottomPanel.transform, false);

                    _startBtn = startBtnTrans.GetComponent<Button>();
                    _startBtn.onClick.RemoveAllListeners();
                    _startBtn.onClick.AddListener(() => { GameManager.Instance.StartBattle(); });
                    
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
                
                // Reconnect buttons - look inside BottomPanel first
                Transform switchBtnTrans = _bottomPanel?.transform.Find("PanelContent/SwitchViewButton")
                    ?? _bottomPanel?.transform.Find("SwitchViewButton")
                    ?? _canvasObj.transform.Find("SwitchViewButton");
                // Delete any old stray SwitchViewButton at canvas root level  
                Transform oldSwitchAtRoot = _canvasObj.transform.Find("SwitchViewButton");
                if (oldSwitchAtRoot != null && _bottomPanel != null && !oldSwitchAtRoot.IsChildOf(_bottomPanel.transform))
                    DestroyImmediate(oldSwitchAtRoot.gameObject);

                if (switchBtnTrans != null)
                {
                    _switchViewBtn = switchBtnTrans.GetComponent<Button>();
                    _switchViewText = switchBtnTrans.Find("Text")?.GetComponent<Text>();
                    _switchViewBtn.onClick.RemoveAllListeners();
                    _switchViewBtn.onClick.AddListener(() => { ToggleView(); });
                }
                
                Transform toggleBtnTrans = _bottomPanel?.transform.Find("PanelContent/TogglePanelButton")
                    ?? _bottomPanel?.transform.Find("TogglePanelButton")
                    ?? _canvasObj.transform.Find("TogglePanelButton");
                // Delete any old stray TogglePanelButton at canvas root level
                Transform oldToggleAtRoot = _canvasObj.transform.Find("TogglePanelButton");
                if (oldToggleAtRoot != null && _bottomPanel != null && !oldToggleAtRoot.IsChildOf(_bottomPanel.transform))
                    DestroyImmediate(oldToggleAtRoot.gameObject);

                _panelContent = _bottomPanel?.transform.Find("PanelContent")?.gameObject;
                _showArrowBtn = _bottomPanel?.transform.Find("ShowArrowButton")?.gameObject;

                if (_showArrowBtn != null)
                {
                    Button arrowBtn = _showArrowBtn.GetComponent<Button>();
                    if (arrowBtn != null)
                    {
                        arrowBtn.onClick.RemoveAllListeners();
                        arrowBtn.onClick.AddListener(() => { ExpandPanel(); });
                    }
                }

                if (toggleBtnTrans != null)
                {
                    _togglePanelBtn = toggleBtnTrans.GetComponent<Button>();
                    Text toggleText = toggleBtnTrans.Find("Text")?.GetComponent<Text>();
                    if (_togglePanelBtn != null)
                    {
                        _togglePanelBtn.onClick.RemoveAllListeners();
                        _togglePanelBtn.onClick.AddListener(() => { TogglePanelCollapse(toggleText); });
                    }
                }
                else
                {
                    Font arial2 = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (arial2 == null) arial2 = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    if (arial2 == null) arial2 = Font.CreateDynamicFontFromOSFont("Arial", 24);
                    CreateTogglePanelButton(_panelContent != null ? _panelContent.transform : _bottomPanel.transform, arial2);
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
                    _gameOverImage = gameOverTrans.Find("GameOverImage")?.GetComponent<Image>();
                    
                    if (_gameOverImage == null)
                    {
                        // Clean up old GameOverPanel content
                        foreach (Transform child in gameOverTrans)
                        {
                            Destroy(child.gameObject);
                        }

                        // Recreate Result Image
                        GameObject goImgObj = new GameObject("GameOverImage");
                        goImgObj.transform.SetParent(gameOverTrans, false);
                        _gameOverImage = goImgObj.AddComponent<Image>();
                        _gameOverImage.preserveAspect = true;
                        RectTransform rtGOImg = _gameOverImage.rectTransform;
                        rtGOImg.anchorMin = new Vector2(0.5f, 0.5f);
                        rtGOImg.anchorMax = new Vector2(0.5f, 0.5f);
                        rtGOImg.anchoredPosition = new Vector2(0, 150); 
                        rtGOImg.sizeDelta = new Vector2(1500, 750);

                        // Recreate Return to Battle Button
                        GameObject returnBtnObj = new GameObject("ReturnToBattleButton");
                        returnBtnObj.transform.SetParent(gameOverTrans, false);
                        Image returnBtnImg = returnBtnObj.AddComponent<Image>();
                        returnBtnImg.sprite = LoadUISprite("ReturnToBattle");
                        returnBtnImg.preserveAspect = false; // Bỏ preserve aspect để các nút đều nhau
                        Button returnBtn = returnBtnObj.AddComponent<Button>();
                        returnBtn.onClick.AddListener(() => {
                            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                        });
                        RectTransform rtReturnBtn = returnBtnObj.GetComponent<RectTransform>();
                        rtReturnBtn.anchorMin = new Vector2(0.5f, 0.5f);
                        rtReturnBtn.anchorMax = new Vector2(0.5f, 0.5f);
                        rtReturnBtn.anchoredPosition = new Vector2(-220, -180);
                        rtReturnBtn.sizeDelta = new Vector2(400, 100);

                        // Recreate Back to Map Button
                        GameObject mapBtnObj = new GameObject("BackToMapButton");
                        mapBtnObj.transform.SetParent(gameOverTrans, false);
                        Image mapBtnImg = mapBtnObj.AddComponent<Image>();
                        mapBtnImg.sprite = LoadUISprite("BackToMap");
                        mapBtnImg.preserveAspect = false; // Bỏ preserve aspect để các nút đều nhau
                        Button mapBtn = mapBtnObj.AddComponent<Button>();
                        mapBtn.onClick.AddListener(() => {
                            UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene"); 
                        });
                        RectTransform rtMapBtn = mapBtnObj.GetComponent<RectTransform>();
                        rtMapBtn.anchorMin = new Vector2(0.5f, 0.5f);
                        rtMapBtn.anchorMax = new Vector2(0.5f, 0.5f);
                        rtMapBtn.anchoredPosition = new Vector2(220, -180);
                        rtMapBtn.sizeDelta = new Vector2(400, 100);
                    }
                }
                
                // Reconnect error banner
                Transform errorBannerTrans = _canvasObj.transform.Find("ErrorBanner");
                if (errorBannerTrans != null)
                {
                    _errorBanner = errorBannerTrans.gameObject;
                    _errorBannerText = errorBannerTrans.Find("Text")?.GetComponent<Text>();
                }

                // Reconnect or create TransitionFog — always start inactive
                Transform existingFog = _canvasObj.transform.Find("TransitionFog");
                if (existingFog != null)
                {
                    _transitionFog = existingFog.GetComponent<Image>();
                }
                else
                {
                    GameObject fogObj = new GameObject("TransitionFog");
                    fogObj.transform.SetParent(_canvasObj.transform, false);
                    _transitionFog = fogObj.AddComponent<Image>();
                    RectTransform rtFog = _transitionFog.rectTransform;
                    rtFog.anchorMin = Vector2.zero;
                    rtFog.anchorMax = Vector2.one;
                    rtFog.sizeDelta = Vector2.zero;
                }
                _transitionFog.color = new Color(0.85f, 0.85f, 0.9f, 0f);
                _transitionFog.raycastTarget = false; // Off by default — only enable during animation
                _transitionFog.gameObject.SetActive(false);
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
        // Only tick error banner timer - don't re-apply UI layout every frame
        if (_errorBannerTimer > 0)
        {
            _errorBannerTimer -= Time.deltaTime;
            if (_errorBannerTimer <= 0 && _errorBanner != null)
            {
                _errorBanner.SetActive(false);
            }
        }
    }

    public void UpdateUIElements()
    {
        // UpdateUIElements is intentionally lightweight now.
        // Layout is set once during CreateUI / UpdatePlacementUI.
        // Only update the panel sprite if panelSprite changes in Inspector.
        if (_bottomPanel != null)
        {
            Image pImg = _bottomPanel.GetComponent<Image>();
            if (pImg != null && panelSprite != null && pImg.sprite != panelSprite)
            {
                pImg.sprite = panelSprite;
                pImg.color = Color.white;
                pImg.type = Image.Type.Sliced;
            }
        }
    }

    // Called by the Editor "Preview UI" button.
    // Reconnects all event listeners WITHOUT touching positions/layout.
    public void CreateUIPreview()
    {
        if (Instance == null) Instance = this;

        _canvasObj = GameObject.Find("UICanvas");
        if (_canvasObj == null)
        {
            // Canvas doesn't exist yet, create fresh
            CreateUI();
            UpdatePlacementUI();
            return;
        }

        // Canvas already exists -- only reconnect references so Play Mode works
        _bottomPanel    = _canvasObj.transform.Find("BottomPanel")?.gameObject;
        _panelContent   = _bottomPanel?.transform.Find("PanelContent")?.gameObject;
        _unitsText      = _canvasObj.transform.Find("UnitsText")?.GetComponent<Text>();
        _placementHint  = _canvasObj.transform.Find("PlacementHint")?.GetComponent<Text>();
        _scoutingReportText = _canvasObj.transform.Find("ScoutingReportText")?.GetComponent<Text>();

        Transform startBtnT = _bottomPanel?.transform.Find("StartButton") ?? _canvasObj.transform.Find("StartButton");
        if (startBtnT != null) _startBtn = startBtnT.GetComponent<Button>();

        Transform switchT = _bottomPanel?.transform.Find("PanelContent/SwitchViewButton")
                         ?? _bottomPanel?.transform.Find("SwitchViewButton")
                         ?? _canvasObj.transform.Find("SwitchViewButton");
        if (switchT != null)
        {
            _switchViewBtn  = switchT.GetComponent<Button>();
            _switchViewText = switchT.Find("Text")?.GetComponent<Text>();
        }

        Transform toggleT = _bottomPanel?.transform.Find("PanelContent/TogglePanelButton")
                          ?? _bottomPanel?.transform.Find("TogglePanelButton")
                          ?? _canvasObj.transform.Find("TogglePanelButton");
        if (toggleT != null) _togglePanelBtn = toggleT.GetComponent<Button>();

        _showArrowBtn = _bottomPanel?.transform.Find("ShowArrowButton")?.gameObject;

        Transform fogT = _canvasObj.transform.Find("TransitionFog");
        if (fogT != null)
        {
            _transitionFog = fogT.GetComponent<Image>();
            _transitionFog.raycastTarget = false;
            _transitionFog.gameObject.SetActive(false);
        }

        Transform gameOverT = _canvasObj.transform.Find("GameOverPanel");
        if (gameOverT != null)
        {
            _gameOverPanel = gameOverT.gameObject;
            _gameOverImage = gameOverT.Find("GameOverImage")?.GetComponent<Image>();
        }

        Transform errorT = _canvasObj.transform.Find("ErrorBanner");
        if (errorT != null)
        {
            _errorBanner     = errorT.gameObject;
            _errorBannerText = errorT.Find("Text")?.GetComponent<Text>();
        }

        Debug.Log("[UIManager] Preview: References reconnected. Layout untouched.");
    }

    // Destroys the preview canvas entirely so you can start fresh.
    public void ClearUIPreview()
    {
        GameObject canvas = GameObject.Find("UICanvas");
        if (canvas != null) DestroyImmediate(canvas);

        GameObject eventSystem = GameObject.Find("EventSystem");
        if (eventSystem != null) DestroyImmediate(eventSystem);
    }

    public void CreateUI_Public() { CreateUI(); UpdatePlacementUI(); }

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
        rtReport.anchoredPosition = new Vector2(-20, -80); // Placed higher since the skill panel is in the map scene
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
        rtPanel.anchorMin = new Vector2(0f, 0f);
        rtPanel.anchorMax = new Vector2(1f, 0f);
        rtPanel.pivot = new Vector2(0.5f, 0f);
        rtPanel.anchoredPosition = Vector2.zero;
        rtPanel.sizeDelta = new Vector2(0f, 350f);

        // ShowArrowButton - uses show.png, anchored at top-center of BottomPanel
        // Always visible; clicking it toggles the panel
        _showArrowBtn = new GameObject("ShowArrowButton");
        _showArrowBtn.transform.SetParent(_bottomPanel.transform, false);
        Image arrowImg = _showArrowBtn.AddComponent<Image>();
        Sprite showSpr = Resources.Load<Sprite>("CustomUI/show");
        if (showSpr != null) { arrowImg.sprite = showSpr; arrowImg.color = Color.white; }
        else arrowImg.color = new Color(0.5f, 0.3f, 0.0f, 0.95f);
        Button arrowBtn = _showArrowBtn.AddComponent<Button>();
        RectTransform rtArrow = _showArrowBtn.GetComponent<RectTransform>();
        rtArrow.anchorMin = new Vector2(0.5f, 1f);
        rtArrow.anchorMax = new Vector2(0.5f, 1f);
        rtArrow.pivot = new Vector2(0.5f, 0f);
        rtArrow.anchoredPosition = new Vector2(0f, 0f); // sits just above the panel top edge
        rtArrow.sizeDelta = new Vector2(120f, 60f);
        arrowBtn.onClick.AddListener(() => { ExpandPanel(); });

        // PanelContent - child that holds buttons and cards; this is what slides/hides
        _panelContent = new GameObject("PanelContent");
        _panelContent.transform.SetParent(_bottomPanel.transform, false);
        RectTransform rtContent = _panelContent.AddComponent<RectTransform>();
        rtContent.anchorMin = Vector2.zero;
        rtContent.anchorMax = Vector2.one;
        rtContent.sizeDelta = Vector2.zero;
        rtContent.anchoredPosition = Vector2.zero;

        _placementHint.text = "Kéo thẻ lên sân để đặt quân.";
        RectTransform rtHint = _placementHint.rectTransform;
        rtHint.anchorMin = new Vector2(0.5f, 0);
        rtHint.anchorMax = new Vector2(0.5f, 0);
        rtHint.pivot = new Vector2(0.5f, 0);
        rtHint.anchoredPosition = new Vector2(0, hintTextPositionY);
        rtHint.sizeDelta = new Vector2(800, 50);

        // Start Button
        GameObject btnObj = new GameObject("StartButton");
        btnObj.transform.SetParent(_bottomPanel.transform, false);
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

        // Switch View Button - inside PanelContent, top-right
        GameObject switchBtnObj = new GameObject("SwitchViewButton");
        switchBtnObj.transform.SetParent(_panelContent.transform, false);
        Image switchImg = switchBtnObj.AddComponent<Image>();
        switchImg.color = new Color(0.4f, 0.15f, 0.0f, 0.95f);
        _switchViewBtn = switchBtnObj.AddComponent<Button>();

        Outline switchOutline = switchBtnObj.AddComponent<Outline>();
        switchOutline.effectColor = new Color(0.85f, 0.65f, 0.1f, 1f);
        switchOutline.effectDistance = new Vector2(2f, 2f);
        
        GameObject switchTextObj = new GameObject("Text");
        switchTextObj.transform.SetParent(switchBtnObj.transform, false);
        _switchViewText = switchTextObj.AddComponent<Text>();
        _switchViewText.font = arial;
        _switchViewText.text = "Xem đội hình tướng địch";
        _switchViewText.fontSize = 22;
        _switchViewText.color = Color.white;
        _switchViewText.alignment = TextAnchor.MiddleCenter;
        RectTransform rtSwitchTxt = _switchViewText.rectTransform;
        rtSwitchTxt.anchorMin = Vector2.zero;
        rtSwitchTxt.anchorMax = Vector2.one;
        rtSwitchTxt.sizeDelta = Vector2.zero;

        RectTransform rtSwitch = switchBtnObj.GetComponent<RectTransform>();
        rtSwitch.anchorMin = new Vector2(0.5f, 1f);
        rtSwitch.anchorMax = new Vector2(1f, 1f);
        rtSwitch.pivot = new Vector2(1f, 1f);
        rtSwitch.anchoredPosition = new Vector2(-10f, 0f);
        rtSwitch.sizeDelta = new Vector2(0f, 55f);

        _switchViewBtn.onClick.AddListener(() => { ToggleView(); });

        // Toggle Panel Button - inside PanelContent, top-left
        CreateTogglePanelButton(_panelContent.transform, arial);

        // Game Over Panel
        _gameOverPanel = new GameObject("GameOverPanel");
        _gameOverPanel.transform.SetParent(_canvasObj.transform, false);
        Image panelImg = _gameOverPanel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.7f);
        RectTransform rtGameOverPanel = _gameOverPanel.GetComponent<RectTransform>();
        rtGameOverPanel.anchorMin = Vector2.zero;
        rtGameOverPanel.anchorMax = Vector2.one;
        rtGameOverPanel.sizeDelta = Vector2.zero;

        // Result Image
        GameObject goImgObj = new GameObject("GameOverImage");
        goImgObj.transform.SetParent(_gameOverPanel.transform, false);
        _gameOverImage = goImgObj.AddComponent<Image>();
        _gameOverImage.preserveAspect = true;
        RectTransform rtGOImg = _gameOverImage.rectTransform;
        rtGOImg.anchorMin = new Vector2(0.5f, 0.5f);
        rtGOImg.anchorMax = new Vector2(0.5f, 0.5f);
        rtGOImg.anchoredPosition = new Vector2(0, 150); 
        rtGOImg.sizeDelta = new Vector2(1500, 750);

        // Return to Battle Button
        GameObject returnBtnObj = new GameObject("ReturnToBattleButton");
        returnBtnObj.transform.SetParent(_gameOverPanel.transform, false);
        Image returnBtnImg = returnBtnObj.AddComponent<Image>();
        returnBtnImg.sprite = LoadUISprite("ReturnToBattle");
        returnBtnImg.preserveAspect = false;
        Button returnBtn = returnBtnObj.AddComponent<Button>();
        returnBtn.onClick.AddListener(() => {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });
        RectTransform rtReturnBtn = returnBtnObj.GetComponent<RectTransform>();
        rtReturnBtn.anchorMin = new Vector2(0.5f, 0.5f);
        rtReturnBtn.anchorMax = new Vector2(0.5f, 0.5f);
        rtReturnBtn.anchoredPosition = new Vector2(-220, -180);
        rtReturnBtn.sizeDelta = new Vector2(400, 100);

        // Back to Map Button
        GameObject mapBtnObj = new GameObject("BackToMapButton");
        mapBtnObj.transform.SetParent(_gameOverPanel.transform, false);
        Image mapBtnImg = mapBtnObj.AddComponent<Image>();
        mapBtnImg.sprite = LoadUISprite("BackToMap");
        mapBtnImg.preserveAspect = false;
        Button mapBtn = mapBtnObj.AddComponent<Button>();
        mapBtn.onClick.AddListener(() => {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene"); 
        });
        RectTransform rtMapBtn = mapBtnObj.GetComponent<RectTransform>();
        rtMapBtn.anchorMin = new Vector2(0.5f, 0.5f);
        rtMapBtn.anchorMax = new Vector2(0.5f, 0.5f);
        rtMapBtn.anchoredPosition = new Vector2(220, -180);
        rtMapBtn.sizeDelta = new Vector2(400, 100);
        
        _gameOverPanel.SetActive(false);

        // Error Banner
        _errorBanner = new GameObject("ErrorBanner");
        _errorBanner.transform.SetParent(_canvasObj.transform, false);
        Image bannerImg = _errorBanner.AddComponent<Image>();
        Sprite showSprite = Resources.Load<Sprite>("CustomUI/show");
        if (showSprite != null) bannerImg.sprite = showSprite;
        else bannerImg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        
        RectTransform rtBanner = _errorBanner.GetComponent<RectTransform>();
        rtBanner.anchorMin = new Vector2(0.5f, 0f);
        rtBanner.anchorMax = new Vector2(0.5f, 0f);
        rtBanner.pivot = new Vector2(0.5f, 0f);
        rtBanner.anchoredPosition = new Vector2(0, 310);
        rtBanner.sizeDelta = new Vector2(600, 150);

        GameObject bannerTextObj = new GameObject("Text");
        bannerTextObj.transform.SetParent(_errorBanner.transform, false);
        _errorBannerText = bannerTextObj.AddComponent<Text>();
        _errorBannerText.font = arial;
        _errorBannerText.fontSize = 24;
        _errorBannerText.color = Color.white;
        _errorBannerText.alignment = TextAnchor.MiddleCenter;
        RectTransform rtBannerText = _errorBannerText.rectTransform;
        rtBannerText.anchorMin = Vector2.zero;
        rtBannerText.anchorMax = Vector2.one;
        rtBannerText.sizeDelta = Vector2.zero;
        
        _errorBanner.SetActive(false);
    }

    public void ShowErrorBanner(string message)
    {
        if (_errorBanner != null && _errorBannerText != null)
        {
            _errorBannerText.text = message;
            _errorBanner.SetActive(true);
            _errorBannerTimer = 3f;
        }
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

    private Coroutine _slideRoutine;

    private void TogglePanelCollapse(Text toggleText)
    {
        _panelCollapsed = !_panelCollapsed;
        if (toggleText != null) toggleText.text = _panelCollapsed ? "Hiện chọn tướng" : "Ẩn chọn tướng";
        
        if (_slideRoutine != null) StopCoroutine(_slideRoutine);

        float targetY = 0f;
        if (_panelCollapsed && _bottomPanel != null && _showArrowBtn != null)
        {
            RectTransform rtPanel = _bottomPanel.GetComponent<RectTransform>();
            RectTransform rtArrow = _showArrowBtn.GetComponent<RectTransform>();
            float anchorY = rtPanel.rect.height * rtArrow.anchorMin.y;
            float arrowBottomY = anchorY + rtArrow.anchoredPosition.y - rtArrow.rect.height * rtArrow.pivot.y;
            targetY = -arrowBottomY;
        }

        _slideRoutine = StartCoroutine(SlidePanelRoutine(targetY));
    }

    private void ExpandPanel()
    {
        if (!_panelCollapsed) return;
        _panelCollapsed = false;
        if (_togglePanelBtn != null)
        {
            Text txt = _togglePanelBtn.transform.Find("Text")?.GetComponent<Text>();
            if (txt != null) txt.text = "Ẩn chọn tướng";
        }
        
        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
        _slideRoutine = StartCoroutine(SlidePanelRoutine(0f));
    }

    private System.Collections.IEnumerator SlidePanelRoutine(float targetY)
    {
        if (_bottomPanel == null) yield break;
        RectTransform rt = _bottomPanel.GetComponent<RectTransform>();
        float startY = rt.anchoredPosition.y;
        float time = 0f;
        float duration = 0.3f; // 300ms animation
        
        if (_showArrowBtn != null)
        {
            // Point UP when collapsed (-350), DOWN when shown (0) -> assumes show.png points UP by default
            float rot = (targetY < -100f) ? 0f : 180f; 
            _showArrowBtn.transform.localRotation = Quaternion.Euler(0, 0, rot);
        }

        while (time < duration)
        {
            time += Time.deltaTime;
            float y = Mathf.Lerp(startY, targetY, time / duration);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            yield return null;
        }
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, targetY);
    }

    private void CreateTogglePanelButton(Transform parent, Font font)
    {
        GameObject toggleBtnObj = new GameObject("TogglePanelButton");
        toggleBtnObj.transform.SetParent(parent, false);
        Image toggleImg = toggleBtnObj.AddComponent<Image>();
        toggleImg.color = new Color(0.4f, 0.15f, 0.0f, 0.95f);
        _togglePanelBtn = toggleBtnObj.AddComponent<Button>();

        Outline toggleOutline = toggleBtnObj.AddComponent<Outline>();
        toggleOutline.effectColor = new Color(0.85f, 0.65f, 0.1f, 1f);
        toggleOutline.effectDistance = new Vector2(2f, 2f);
        
        GameObject toggleTextObj = new GameObject("Text");
        toggleTextObj.transform.SetParent(toggleBtnObj.transform, false);
        Text toggleText = toggleTextObj.AddComponent<Text>();
        toggleText.font = font;
        toggleText.text = "Ẩn chọn tướng";
        toggleText.fontSize = 22;
        toggleText.color = Color.white;
        toggleText.alignment = TextAnchor.MiddleCenter;
        RectTransform rtTxt = toggleText.rectTransform;
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.sizeDelta = Vector2.zero;

        RectTransform rtToggle = toggleBtnObj.GetComponent<RectTransform>();
        rtToggle.anchorMin = new Vector2(0f, 1f);
        rtToggle.anchorMax = new Vector2(0.5f, 1f);
        rtToggle.pivot = new Vector2(0f, 1f);
        rtToggle.anchoredPosition = new Vector2(10f, 0f);
        rtToggle.sizeDelta = new Vector2(-10f, 55f);

        _togglePanelBtn.onClick.AddListener(() => { TogglePanelCollapse(toggleText); });
    }

    private void ToggleView()
    {
        if (CameraController.Instance == null) return;

        CameraView targetView = CameraView.EnemySetup;
        string newText = "Sắp xếp đội hình";

        if (CameraController.Instance.GetCurrentView() == CameraView.EnemySetup)
        {
            targetView = CameraView.PlayerSetup;
            newText = "Xem đội hình tướng địch";
        }

        StartCoroutine(SwitchViewRoutine(targetView, newText));
    }

    private System.Collections.IEnumerator SwitchViewRoutine(CameraView targetView, string newButtonText)
    {
        // Create fog lazily if somehow missing
        if (_transitionFog == null)
        {
            GameObject fogObj = new GameObject("TransitionFog");
            if (_canvasObj != null) fogObj.transform.SetParent(_canvasObj.transform, false);
            _transitionFog = fogObj.AddComponent<Image>();
            RectTransform rtFog = _transitionFog.rectTransform;
            rtFog.anchorMin = Vector2.zero;
            rtFog.anchorMax = Vector2.one;
            rtFog.sizeDelta = Vector2.zero;
        }

        _transitionFog.color = new Color(0.85f, 0.85f, 0.9f, 0f);
        _transitionFog.raycastTarget = true;  // Block clicks during animation
        _transitionFog.gameObject.SetActive(true);
        _transitionFog.transform.SetAsLastSibling();

        float duration = 0.4f;
        float elapsed = 0f;
        Color c = _transitionFog.color;

        // Fade in: transparent -> opaque white fog
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(elapsed / duration);
            _transitionFog.color = c;
            yield return null;
        }
        c.a = 1f;
        _transitionFog.color = c;

        // Switch camera view during full white-out
        CameraController.Instance.SetView(targetView);
        if (_switchViewText != null) _switchViewText.text = newButtonText;
        UpdatePlacementUI();

        yield return new WaitForSecondsRealtime(0.15f);

        // Fade out: opaque -> transparent
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(1f - elapsed / duration);
            _transitionFog.color = c;
            yield return null;
        }
        c.a = 0f;
        _transitionFog.color = c;
        _transitionFog.raycastTarget = false;  // Stop blocking input when invisible
        _transitionFog.gameObject.SetActive(false);
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
            Transform cardParent = (_panelContent != null) ? _panelContent.transform : _bottomPanel.transform;

            if (isPlayer)
            {
                // Only create cards if they don't already exist (preserve Inspector layout)
                bool cardsExist = false;
                foreach (Transform child in cardParent)
                {
                    if (child.name.StartsWith("UnitCard"))
                    {
                        cardsExist = true;
                        break;
                    }
                }

                if (!cardsExist)
                {
                    int numCards = 3;
                    float cardSpacing = 420f;
                    float startX = -((numCards - 1) * cardSpacing) / 2f;

                    for (int i = 0; i < numCards; i++)
                    {
                        int typeIndex = i;
                        GameObject cardObj = new GameObject("UnitCard_" + typeIndex);
                        cardObj.transform.SetParent(cardParent, false);
                        Image cardImg = cardObj.AddComponent<Image>();
                        cardImg.color = Color.white;

                        string spriteName = "";
                        if (typeIndex == 0) spriteName = "CustomUI/BoBinhCard";
                        else if (typeIndex == 1) spriteName = "CustomUI/CungThuCard";
                        else if (typeIndex == 2) spriteName = "CustomUI/TuongQuanCard";

                        Sprite spr = Resources.Load<Sprite>(spriteName);
                        if (spr != null) cardImg.sprite = spr;

                        RectTransform rtCard = cardObj.GetComponent<RectTransform>();
                        rtCard.anchorMin = new Vector2(0.5f, 0.5f);
                        rtCard.anchorMax = new Vector2(0.5f, 0.5f);
                        rtCard.pivot = new Vector2(0.5f, 0.5f);
                        // Y=-30 centers cards in lower area below the 55px button bar
                        rtCard.anchoredPosition = new Vector2(startX + i * cardSpacing, -30f);
                        rtCard.sizeDelta = new Vector2(380f, 230f);

                        var ddc = cardObj.AddComponent<DragDropCard>();
                        ddc.unitTypeIndex = typeIndex;
                    }
                }
            }
            
            // Toggle visibility of unit cards based on whether it is player setup
            foreach (Transform child in cardParent)
            {
                if (child.name.StartsWith("UnitCard"))
                {
                    child.gameObject.SetActive(isPlayer);
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
                    _switchViewText.text = "🔒 Xem đội hình (Lv3)";
                }
            }
            else
            {
                _switchViewBtn.interactable = true;
                if (_switchViewText != null)
                {
                    _switchViewText.text = (CameraController.Instance != null && CameraController.Instance.GetCurrentView() == CameraView.EnemySetup)
                        ? "Sắp xếp đội hình" 
                        : "Xem đội hình tướng địch";
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
            _gameOverImage.sprite = LoadUISprite("Victory");
        }
        else
        {
            _gameOverImage.sprite = LoadUISprite("Defeat");
        }
    }

    private Sprite LoadUISprite(string name)
    {
        Sprite s = Resources.Load<Sprite>("UI/" + name);
        if (s != null) return s;
        Texture2D tex = Resources.Load<Texture2D>("UI/" + name);
        if (tex != null) return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return null;
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
            EditorGUILayout.HelpBox(
                "▶ Reconnect References: Giữ nguyên toàn bộ layout/vị trí bạn đã chỉnh. Chỉ kết nối lại code references.\n" +
                "✖ Clear & Rebuild: XÓA toàn bộ UICanvas và tạo lại từ script (mất layout đã chỉnh).",
                MessageType.Info);

            if (GUILayout.Button("▶  Reconnect References (Giữ layout)", GUILayout.Height(36)))
            {
                manager.CreateUIPreview();
            }

            if (GUILayout.Button("✖  Clear & Rebuild từ Script", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("Xác nhận",
                    "Hành động này sẽ XÓA toàn bộ UICanvas và tạo lại từ đầu.\nMọi vị trí bạn đã chỉnh sẽ bị mất!",
                    "Xóa và tạo lại", "Hủy"))
                {
                    manager.ClearUIPreview();
                    manager.CreateUI_Public();
                }
            }
        }
    }
}
#endif
