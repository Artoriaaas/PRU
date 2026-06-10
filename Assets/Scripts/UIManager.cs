using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panel Settings")]
    public Sprite panelSprite;
    public Vector2 panelSize = new Vector2(680f, 72f); // Default height is 1/3 of 218
    public Vector2 panelPosition = new Vector2(0f, 10f);

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
    private Button _switchViewBtn;
    private Text _switchViewText;

    private void Reset()
    {
#if UNITY_EDITOR
        panelSprite = Resources.Load<Sprite>("CardPanel");
#endif
    }

    void Awake()
    {
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
                
                Transform startBtnTrans = _canvasObj.transform.Find("StartButton");
                if (startBtnTrans != null)
                {
                    _startBtn = startBtnTrans.GetComponent<Button>();
                    _startBtn.onClick.RemoveAllListeners();
                    _startBtn.onClick.AddListener(() => {
                        GameManager.Instance.StartBattle();
                    });
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
            Destroy(gameObject);
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
                rtPanel.sizeDelta = panelSize;
                
                Image pImg = _bottomPanel.GetComponent<Image>();
                if (pImg != null)
                {
                    Sprite activeSprite = panelSprite != null ? panelSprite : Resources.Load<Sprite>("CardPanel");
                    if (activeSprite != null)
                    {
                        pImg.sprite = activeSprite;
                        pImg.color = Color.white;
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
        _canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
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

        // Placement Hint
        GameObject hintObj = new GameObject("PlacementHint");
        hintObj.transform.SetParent(_canvasObj.transform, false);
        _placementHint = hintObj.AddComponent<Text>();
        _placementHint.font = arial;
        _placementHint.fontSize = 20;
        _placementHint.color = Color.yellow;
        _placementHint.alignment = TextAnchor.LowerCenter;
        
        // Bottom Panel
        _bottomPanel = new GameObject("BottomPanel");
        _bottomPanel.transform.SetParent(_canvasObj.transform, false);
        Image pImg = _bottomPanel.AddComponent<Image>();
        Sprite activeSprite = panelSprite != null ? panelSprite : Resources.Load<Sprite>("CardPanel");
        if (activeSprite != null)
        {
            pImg.sprite = activeSprite;
            pImg.color = Color.white;
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
        rtPanel.sizeDelta = panelSize;

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
        btnImg.color = new Color(0.2f, 0.8f, 0.2f);
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

        RectTransform rtBtn = btnObj.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(1, 0);
        rtBtn.anchorMax = new Vector2(1, 0);
        rtBtn.pivot = new Vector2(1, 0);
        rtBtn.anchoredPosition = new Vector2(-20, 20);
        rtBtn.sizeDelta = new Vector2(160, 50);

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

    private void ToggleView()
    {
        if (CameraController.Instance == null) return;

        if (CameraController.Instance.GetCurrentView() == CameraView.PlayerSetup)
        {
            CameraController.Instance.SetView(CameraView.EnemySetup);
            _switchViewText.text = "< View Player";
            
            _bottomPanel.SetActive(false);
            _placementHint.gameObject.SetActive(false);
            _unitsText.gameObject.SetActive(false);
            _startBtn.gameObject.SetActive(false);
        }
        else if (CameraController.Instance.GetCurrentView() == CameraView.EnemySetup)
        {
            CameraController.Instance.SetView(CameraView.PlayerSetup);
            _switchViewText.text = "View Enemy >";
            
            _bottomPanel.SetActive(true);
            _placementHint.gameObject.SetActive(true);
            _unitsText.gameObject.SetActive(true);
            _startBtn.gameObject.SetActive(true);
        }
    }

    public void UpdatePlacementUI()
    {
        if (GameManager.Instance != null)
        {
            int remaining = GameManager.Instance.maxPlayerUnits - GameManager.Instance.placedPlayerUnits;
            _unitsText.text = $"Available Units: {remaining}";
        }
    }

    public void HidePlacementUI()
    {
        if (_unitsText != null) _unitsText.gameObject.SetActive(false);
        if (_startBtn != null) _startBtn.gameObject.SetActive(false);
        if (_placementHint != null) _placementHint.gameObject.SetActive(false);
        if (_bottomPanel != null) _bottomPanel.SetActive(false);
        if (_switchViewBtn != null) _switchViewBtn.gameObject.SetActive(false);
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
