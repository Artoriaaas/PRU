using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkillHoverDetector : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    public string description;
    public SkillManager manager;

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (manager != null)
        {
            manager.ShowTooltip(description, transform.position);
        }
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (manager != null)
        {
            manager.HideTooltip();
        }
    }
}

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [Header("Skill Point State")]
    private static int _skillPoints = 5;
    public int skillPoints
    {
        get => _skillPoints;
        set => _skillPoints = value;
    }

    [Header("Skill Levels")]
    private static int _barracksLevel = 0;
    public int barracksLevel
    {
        get => _barracksLevel;
        set => _barracksLevel = value;
    }

    private static int _troopLevel = 0;
    public int troopLevel
    {
        get => _troopLevel;
        set => _troopLevel = value;
    }

    private static int _scoutingLevel = 0;
    public int scoutingLevel
    {
        get => _scoutingLevel;
        set => _scoutingLevel = value;
    }

    private static int _logisticsLevel = 0;
    public int logisticsLevel
    {
        get => _logisticsLevel;
        set => _logisticsLevel = value;
    }

    [Header("UI References")]
    private GameObject _skillPanelObj;
    private Text _pointsText;
    
    // Store buttons to update visuals dynamically
    private Dictionary<string, Button[]> _skillButtons = new Dictionary<string, Button[]>();

    [Header("Tooltip References")]
    private GameObject _tooltipPanelObj;
    private Text _tooltipText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    private void Start()
    {
        Debug.Log("SkillManager: Start called. MapSceneBootstrapper present: " + (Object.FindAnyObjectByType<MapSceneBootstrapper>() != null));
        if (Object.FindAnyObjectByType<MapSceneBootstrapper>() != null)
        {
            CreateSkillUI();
        }
    }

    private void OnDestroy()
    {
        if (_tooltipPanelObj != null) Destroy(_tooltipPanelObj);
    }

    public void ResetSkills()
    {
        skillPoints = 5;
        barracksLevel = 0;
        troopLevel = 0;
        scoutingLevel = 0;
        logisticsLevel = 0;
        
        UpdateUI();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlacementUI();
        }
    }

    public void ToggleSkillPanel()
    {
        if (_skillPanelObj != null)
        {
            _skillPanelObj.SetActive(!_skillPanelObj.activeSelf);
        }
    }

    public bool TryUpgrade(string skillType, int targetLevel)
    {
        if (skillPoints <= 0) return false;

        int currentLevel = 0;
        if (skillType == "Barracks") currentLevel = barracksLevel;
        else if (skillType == "Troop") currentLevel = troopLevel;
        else if (skillType == "Scouting") currentLevel = scoutingLevel;
        else if (skillType == "Logistics") currentLevel = logisticsLevel;

        if (targetLevel != currentLevel + 1) return false; // Must upgrade sequentially

        skillPoints--;

        if (skillType == "Barracks") barracksLevel = targetLevel;
        else if (skillType == "Troop") troopLevel = targetLevel;
        else if (skillType == "Scouting") scoutingLevel = targetLevel;
        else if (skillType == "Logistics") logisticsLevel = targetLevel;

        // Immediately update description inside tooltip if upgraded while hovering
        HideTooltip();

        UpdateUI();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlacementUI();
        }
        return true;
    }

    public string GetScoutingReport()
    {
        if (GameManager.Instance == null || GameManager.Instance.activeLevel == null)
        {
            return "Báo cáo trinh thám: Không tìm thấy dữ liệu bàn chơi.";
        }

        int count = GameManager.Instance.activeLevel.enemyPlacements.Count;

        switch (scoutingLevel)
        {
            case 0:
                return "Báo cáo trinh thám:\n🔒 Chưa kích hoạt trinh thám. Hãy nâng cấp kỹ năng để biết thông tin.";
            case 1:
                return $"Báo cáo trinh thám (Lv1):\n👥 Phát hiện có {count} quân địch trên bản đồ.";
            case 2:
                string composition = "";
                if (count == 8)
                {
                    composition = "5 Kiếm sĩ, 3 Cung thủ";
                }
                else
                {
                    int swords = count / 2 + 1;
                    int archers = count - swords;
                    composition = $"{swords} Kiếm sĩ, {archers} Cung thủ";
                }
                return $"Báo cáo trinh thám (Lv2):\n👥 Có {count} quân địch. Gồm: {composition}.";
            case 3:
                return "Báo cáo trinh thám (Lv3):\n👁️ Đã định vị chính xác vị trí đội hình địch. Bạn có thể sử dụng nút 'Xem Địch' để theo dõi.";
            default:
                return "";
        }
    }

    public string GetDescription(string skillType, int level)
    {
        if (skillType == "Barracks")
        {
            switch (level)
            {
                case 1: return "Doanh trại Cấp 1:\nTăng thêm 1 quân tối đa có thể triển khai trên sân đấu.";
                case 2: return "Doanh trại Cấp 2:\nTăng thêm 2 quân tối đa có thể triển khai trên sân đấu.";
                case 3: return "Doanh trại Cấp 3:\nTăng thêm 3 quân tối đa có thể triển khai trên sân đấu.";
            }
        }
        else if (skillType == "Troop")
        {
            switch (level)
            {
                case 1: return "Quân chủng Cấp 1:\nMở khóa việc chiêu mộ binh chủng Cung Thủ (Archer).";
                case 2: return "Quân chủng Cấp 2:\nMở khóa việc chiêu mộ binh chủng Kỵ Binh (Cavalry).";
                case 3: return "Quân chủng Cấp 3:\nMở khóa binh chủng cận chiến đặc biệt: Hổ Bôn Quân.";
            }
        }
        else if (skillType == "Scouting")
        {
            switch (level)
            {
                case 1: return "Trinh thám Cấp 1:\nBiết tổng số lượng lính phe địch trong trận đấu.";
                case 2: return "Trinh thám Cấp 2:\nBiết cơ cấu các loại binh chủng phe địch gồm những quân gì.";
                case 3: return "Trinh thám Cấp 3:\nLộ diện vị trí đội hình phe địch (mở khóa nút Xem Địch).";
            }
        }
        else if (skillType == "Logistics")
        {
            switch (level)
            {
                case 1: return "Hậu cần Cấp 1:\nTăng 10% các chỉ số (HP, Công, Thủ) cho toàn bộ quân ta.";
                case 2: return "Hậu cần Cấp 2:\nTăng 20% các chỉ số (HP, Công, Thủ) cho toàn bộ quân ta.";
                case 3: return "Hậu cần Cấp 3:\nTăng 30% các chỉ số (HP, Công, Thủ) cho toàn bộ quân ta.";
            }
        }
        return "";
    }

    public void ShowTooltip(string descriptionText, Vector3 buttonPosition)
    {
        if (_tooltipPanelObj == null || _tooltipText == null) return;

        _tooltipText.text = descriptionText;
        _tooltipPanelObj.SetActive(true);

        // Position it to the left and slightly below the button to prevent edge cutoff
        _tooltipPanelObj.transform.position = buttonPosition + new Vector3(-170f, -60f, 0f);
    }

    public void HideTooltip()
    {
        if (_tooltipPanelObj != null)
        {
            _tooltipPanelObj.SetActive(false);
        }
    }

    private Font GetSafeFont()
    {
        Font font = null;
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch {}
        
        if (font == null)
        {
            try
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch {}
        }
        
        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        }
        return font;
    }

    private void CreateSkillUI()
    {
        Debug.Log("SkillManager: CreateSkillUI called");
        GameObject canvasObj = GameObject.Find("UICanvas");
        if (canvasObj == null)
        {
            canvasObj = GameObject.Find("Canvas");
        }
        if (canvasObj == null)
        {
            Canvas c = Object.FindAnyObjectByType<Canvas>();
            if (c != null) canvasObj = c.gameObject;
        }
        Debug.Log("SkillManager: Canvas found: " + (canvasObj != null ? canvasObj.name : "null"));
        if (canvasObj == null) return;

        Font font = GetSafeFont();

        // 0. Toggle Button
        GameObject toggleBtnObj = new GameObject("ToggleSkillBtn");
        toggleBtnObj.transform.SetParent(canvasObj.transform, false);
        Image tBtnImg = toggleBtnObj.AddComponent<Image>();
        tBtnImg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        Button toggleBtn = toggleBtnObj.AddComponent<Button>();
        toggleBtn.onClick.AddListener(ToggleSkillPanel);

        GameObject tBtnTextObj = new GameObject("Text");
        tBtnTextObj.transform.SetParent(toggleBtnObj.transform, false);
        Text tBtnText = tBtnTextObj.AddComponent<Text>();
        tBtnText.font = font;
        tBtnText.text = "Kỹ Năng";
        tBtnText.fontSize = 16;
        tBtnText.color = Color.white;
        tBtnText.alignment = TextAnchor.MiddleCenter;
        tBtnText.rectTransform.sizeDelta = new Vector2(100, 40);

        RectTransform rtToggleBtn = toggleBtnObj.GetComponent<RectTransform>();
        rtToggleBtn.anchorMin = new Vector2(0.5f, 1);
        rtToggleBtn.anchorMax = new Vector2(0.5f, 1);
        rtToggleBtn.pivot = new Vector2(0.5f, 1);
        rtToggleBtn.anchoredPosition = new Vector2(0, -20);
        rtToggleBtn.sizeDelta = new Vector2(120, 50);

        // 1. Skill Panel Container (Height increased to fit 4 rows and points)
        _skillPanelObj = new GameObject("SkillPanel");
        _skillPanelObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = _skillPanelObj.AddComponent<Image>();
        bgImg.color = Color.white; 
        Sprite questPanelSprite = Resources.Load<Sprite>("QuestPanel");
        if (questPanelSprite != null)
        {
            bgImg.sprite = questPanelSprite;
        }
        else
        {
            bgImg.color = new Color(0.12f, 0.08f, 0.08f, 0.95f); // Fallback
        }

        RectTransform rtPanel = _skillPanelObj.GetComponent<RectTransform>();
        rtPanel.anchoredPosition = new Vector2(0, -100); // Canh giữa màn hình
        rtPanel.pivot = new Vector2(0.5f, 1); // Pivot ở giữa trên cùng
        rtPanel.anchorMin = new Vector2(0.5f, 1);
        rtPanel.anchorMax = new Vector2(0.5f, 1);
        rtPanel.sizeDelta = new Vector2(400, 300);
        rtPanel.localScale = new Vector3(2f, 2f, 2f); // Tăng kích thước gấp đôi

        // Title text
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_skillPanelObj.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = font;
        titleText.text = "BẢNG KỸ NĂNG";
        titleText.fontSize = 20;
        titleText.color = new Color(0.95f, 0.85f, 0.55f);
        titleText.alignment = TextAnchor.MiddleCenter;
        RectTransform rtTitle = titleText.rectTransform;
        rtTitle.anchorMin = new Vector2(0f, 1f);
        rtTitle.anchorMax = new Vector2(1f, 1f);
        rtTitle.pivot = new Vector2(0.5f, 1f);
        rtTitle.anchoredPosition = new Vector2(0, -10);
        rtTitle.sizeDelta = new Vector2(0, 30);

        // 2. Generate rows for each of the 4 skills
        CreateSkillRow("Doanh trại", "Barracks", -65, font);
        CreateSkillRow("Quân chủng", "Troop", -105, font);
        CreateSkillRow("Trinh thám", "Scouting", -145, font);
        CreateSkillRow("Hậu cần", "Logistics", -185, font);

        // 3. Points and Reset at the bottom
        GameObject pointsObj = new GameObject("PointsText");
        pointsObj.transform.SetParent(_skillPanelObj.transform, false);
        _pointsText = pointsObj.AddComponent<Text>();
        _pointsText.font = font;
        _pointsText.fontSize = 15;
        _pointsText.color = Color.white;
        _pointsText.alignment = TextAnchor.MiddleRight;
        RectTransform rtPoints = _pointsText.rectTransform;
        rtPoints.anchorMin = new Vector2(1f, 1f);
        rtPoints.anchorMax = new Vector2(1f, 1f);
        rtPoints.pivot = new Vector2(1f, 1f);
        rtPoints.anchoredPosition = new Vector2(-15, -230);
        rtPoints.sizeDelta = new Vector2(200, 25);

        GameObject resetBtnObj = new GameObject("ResetButton");
        resetBtnObj.transform.SetParent(_skillPanelObj.transform, false);
        Image rImg = resetBtnObj.AddComponent<Image>();
        rImg.color = new Color(0.4f, 0.15f, 0.15f);
        Button resetBtn = resetBtnObj.AddComponent<Button>();
        resetBtn.onClick.AddListener(ResetSkills);
        
        GameObject rTextObj = new GameObject("Text");
        rTextObj.transform.SetParent(resetBtnObj.transform, false);
        Text rText = rTextObj.AddComponent<Text>();
        rText.font = font;
        rText.text = "Reset";
        rText.fontSize = 13;
        rText.color = Color.white;
        rText.alignment = TextAnchor.MiddleCenter;
        rText.rectTransform.sizeDelta = new Vector2(70, 25);

        RectTransform rtReset = resetBtnObj.GetComponent<RectTransform>();
        rtReset.anchorMin = new Vector2(0f, 1f);
        rtReset.anchorMax = new Vector2(0f, 1f);
        rtReset.pivot = new Vector2(0f, 1f);
        rtReset.anchoredPosition = new Vector2(35, -230);
        rtReset.sizeDelta = new Vector2(80, 25);

        // 4. Floating Tooltip Panel Setup
        _tooltipPanelObj = new GameObject("SkillTooltip");
        _tooltipPanelObj.transform.SetParent(canvasObj.transform, false);
        Image tBg = _tooltipPanelObj.AddComponent<Image>();
        tBg.color = new Color(0.06f, 0.06f, 0.08f, 0.98f);
        tBg.raycastTarget = false; // Never block mouse raycasts

        Outline tOutline = _tooltipPanelObj.AddComponent<Outline>();
        tOutline.effectColor = new Color(0.95f, 0.85f, 0.55f, 0.8f);
        tOutline.effectDistance = new Vector2(1f, 1f);

        RectTransform rtTooltip = _tooltipPanelObj.GetComponent<RectTransform>();
        rtTooltip.anchorMin = Vector2.zero;
        rtTooltip.anchorMax = Vector2.zero;
        rtTooltip.pivot = new Vector2(0f, 1f);
        rtTooltip.sizeDelta = new Vector2(280, 85);

        GameObject tTextObj = new GameObject("Text");
        tTextObj.transform.SetParent(_tooltipPanelObj.transform, false);
        _tooltipText = tTextObj.AddComponent<Text>();
        _tooltipText.font = font;
        _tooltipText.fontSize = 13;
        _tooltipText.color = Color.white;
        _tooltipText.alignment = TextAnchor.UpperLeft;
        _tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _tooltipText.verticalOverflow = VerticalWrapMode.Truncate;
        _tooltipText.raycastTarget = false;

        RectTransform rtTText = _tooltipText.rectTransform;
        rtTText.anchorMin = Vector2.zero;
        rtTText.anchorMax = Vector2.one;
        rtTText.offsetMin = new Vector2(10, 10);
        rtTText.offsetMax = new Vector2(-10, -10);

        _tooltipPanelObj.SetActive(false);
        _skillPanelObj.SetActive(false); // Start inactive by default

        UpdateUI();
    }

    private void CreateSkillRow(string displayName, string skillType, float yOffset, Font font)
    {
        // Label
        GameObject labelObj = new GameObject(skillType + "_Label");
        labelObj.transform.SetParent(_skillPanelObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.font = font;
        labelText.text = displayName + ":";
        labelText.fontSize = 15;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        RectTransform rtLabel = labelText.rectTransform;
        rtLabel.anchorMin = new Vector2(0f, 1f);
        rtLabel.anchorMax = new Vector2(0.35f, 1f);
        rtLabel.pivot = new Vector2(0f, 1f);
        rtLabel.anchoredPosition = new Vector2(35, yOffset);
        rtLabel.sizeDelta = new Vector2(0, 30);

        // Buttons
        Button[] buttons = new Button[3];
        buttons[0] = CreateCircleButton(skillType + "_Btn1", new Vector2(145, yOffset), skillType, 1);
        buttons[1] = CreateCircleButton(skillType + "_Btn2", new Vector2(195, yOffset), skillType, 2);
        buttons[2] = CreateCircleButton(skillType + "_Btn3", new Vector2(245, yOffset), skillType, 3);

        _skillButtons.Add(skillType, buttons);
    }

    private Button CreateCircleButton(string name, Vector2 pos, string skillType, int level)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(_skillPanelObj.transform, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f);
        
        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = Color.gray;
        outline.effectDistance = new Vector2(1f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => TryUpgrade(skillType, level));

        // Attach hover detector
        var hover = btnObj.AddComponent<SkillHoverDetector>();
        hover.manager = this;
        hover.description = GetDescription(skillType, level);

        GameObject tObj = new GameObject("Text");
        tObj.transform.SetParent(btnObj.transform, false);
        Text text = tObj.AddComponent<Text>();
        text.font = GetSafeFont();
        text.text = "○";
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.rectTransform.sizeDelta = new Vector2(30, 30);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(30, 30);

        return btn;
    }

    private void UpdateUI()
    {
        if (_pointsText != null)
        {
            _pointsText.text = $"Điểm kỹ năng: {skillPoints}";
        }

        UpdateRowButtons("Barracks", barracksLevel);
        UpdateRowButtons("Troop", troopLevel);
        UpdateRowButtons("Scouting", scoutingLevel);
        UpdateRowButtons("Logistics", logisticsLevel);
    }

    private void UpdateRowButtons(string skillType, int currentLevel)
    {
        if (!_skillButtons.ContainsKey(skillType)) return;
        Button[] buttons = _skillButtons[skillType];

        for (int i = 0; i < 3; i++)
        {
            int level = i + 1;
            Button btn = buttons[i];
            if (btn == null) continue;

            Text txt = btn.transform.Find("Text")?.GetComponent<Text>();
            Image img = btn.GetComponent<Image>();
            Outline outline = btn.GetComponent<Outline>();
            SkillHoverDetector hover = btn.GetComponent<SkillHoverDetector>();

            // Sync description just in case state changes
            if (hover != null)
            {
                hover.description = GetDescription(skillType, level);
            }

            if (txt != null)
            {
                txt.text = currentLevel >= level ? "●" : "○";
            }

            if (img != null)
            {
                if (currentLevel >= level)
                {
                    img.color = new Color(0.85f, 0.65f, 0.15f); // Unlocked - Gold
                    if (outline != null) outline.effectColor = new Color(1f, 0.9f, 0.5f);
                }
                else if (currentLevel + 1 == level && skillPoints > 0)
                {
                    img.color = new Color(0.4f, 0.4f, 0.4f); // Upgradeable - Gray highlight
                    if (outline != null) outline.effectColor = Color.yellow;
                }
                else
                {
                    img.color = new Color(0.2f, 0.2f, 0.2f); // Locked - Dark gray
                    if (outline != null) outline.effectColor = Color.gray;
                }
            }
        }
    }
}
