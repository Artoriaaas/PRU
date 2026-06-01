using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private GameObject _canvasObj;
    private Text _unitsText;
    private Button _startBtn;
    private GameObject _gameOverPanel;
    private Text _gameOverText;
    private Text _placementHint;
    
    private GameObject _bottomPanel;
    private Button _switchViewBtn;
    private Text _switchViewText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            CreateUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void CreateUI()
    {
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif

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
        pImg.color = new Color(0, 0, 0, 0.5f);
        RectTransform rtPanel = _bottomPanel.GetComponent<RectTransform>();
        rtPanel.anchorMin = new Vector2(0, 0);
        rtPanel.anchorMax = new Vector2(1, 0);
        rtPanel.pivot = new Vector2(0.5f, 0);
        rtPanel.anchoredPosition = Vector2.zero;
        rtPanel.sizeDelta = new Vector2(0, 120);

        // Unit Card
        GameObject cardObj = new GameObject("UnitCard");
        cardObj.transform.SetParent(_bottomPanel.transform, false);
        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.color = Color.blue;
        RectTransform rtCard = cardObj.GetComponent<RectTransform>();
        rtCard.anchorMin = new Vector2(0.5f, 0.5f);
        rtCard.anchorMax = new Vector2(0.5f, 0.5f);
        rtCard.anchoredPosition = Vector2.zero;
        rtCard.sizeDelta = new Vector2(80, 80);
        
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
        cardText.rectTransform.sizeDelta = new Vector2(80, 80);

        _placementHint.text = "Drag the blue card onto the grid to place units.";
        RectTransform rtHint = _placementHint.rectTransform;
        rtHint.anchorMin = new Vector2(0.5f, 0);
        rtHint.anchorMax = new Vector2(0.5f, 0);
        rtHint.pivot = new Vector2(0.5f, 0);
        rtHint.anchoredPosition = new Vector2(0, 130);
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
