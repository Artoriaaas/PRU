using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    [Header("Dialogue Config")]
    [Tooltip("Drag a DialogueData ScriptableObject here to configure opening dialogue. If empty, falls back to default opening dialog.")]
    public DialogueData dialogueData;

    [Header("UI References")]
    public GameObject dialogueOverlay;
    public Image leftPortrait;
    public Image rightPortrait;
    public Text nameText;
    public Text dialogueText;

    [Header("Sprites")]
    public Sprite characterASprite;
    public Sprite characterBSprite;

    [Header("Dialogue Content")]
    public List<DialogueNode> dialogueNodes = new List<DialogueNode>();

    [Header("Settings")]
    public float typeSpeed = 0.03f;
    public float transitionSpeed = 8f;

    private int currentNodeIndex = -1;
    private bool isTyping = false;
    private string fullTextToShow = "";
    private Coroutine typingCoroutine;
    
    // Lerp targets for portraits
    private Vector3 leftTargetScale = Vector3.one;
    private Color leftTargetColor = Color.white;
    private Vector3 rightTargetScale = Vector3.one;
    private Color rightTargetColor = Color.white;

    private MapCameraController mapCamera;

    void Awake()
    {
        // Force-load sprites from Resources to fix null/inspector issues
        characterASprite = Resources.Load<Sprite>("CustomUI/TuongQuan");
        characterBSprite = Resources.Load<Sprite>("CustomUI/TuongDich");

        // Ensure portraits fill edge-to-edge but are 2x larger and wider
        if (leftPortrait != null)
        {
            leftPortrait.preserveAspect = false;
            RectTransform rtL = leftPortrait.GetComponent<RectTransform>();
            rtL.anchorMin = new Vector2(0f, 0f);
            rtL.anchorMax = new Vector2(0f, 0f);
            rtL.pivot = new Vector2(0f, 0f);
            rtL.anchoredPosition = Vector2.zero; // Bottom-left corner
            rtL.sizeDelta = new Vector2(1210f, 1500f); // Tăng bề ngang thêm 10%
        }
        if (rightPortrait != null)
        {
            rightPortrait.preserveAspect = false;
            RectTransform rtR = rightPortrait.GetComponent<RectTransform>();
            rtR.anchorMin = new Vector2(1f, 0f);
            rtR.anchorMax = new Vector2(1f, 0f);
            rtR.pivot = new Vector2(1f, 0f);
            rtR.anchoredPosition = Vector2.zero; // Bottom-right corner
            rtR.sizeDelta = new Vector2(1210f, 1500f); // Tăng bề ngang thêm 10%
        }
    }

    void Start()
    {
        mapCamera = FindAnyObjectByType<MapCameraController>();
        
        // Load dialogue content from ScriptableObject if assigned
        if (dialogueData != null)
        {
            if (dialogueData.characterASprite != null) characterASprite = dialogueData.characterASprite;
            if (dialogueData.characterBSprite != null) characterBSprite = dialogueData.characterBSprite;
            
            dialogueNodes.Clear();
            foreach (var node in dialogueData.dialogueNodes)
            {
                dialogueNodes.Add(node);
            }
        }

        // Setup initial default dialogue nodes as fallback
        if (dialogueNodes.Count == 0)
        {
            dialogueNodes.Add(new DialogueNode
            {
                speakerName = "Tướng Quân A",
                text = "Báo cáo! Quân ta đã tiến sát thành Thăng Long. Trận chiến quyết định sắp bắt đầu!",
                isCharacterA = true
            });
            dialogueNodes.Add(new DialogueNode
            {
                speakerName = "Cung Thủ B",
                text = "Hãy cẩn trọng, thưa Tướng quân. Quân địch bố trí rất nhiều cung thủ mai phục ở vách đá hiểm trở phía trước.",
                isCharacterA = false
            });
            dialogueNodes.Add(new DialogueNode
            {
                speakerName = "Tướng Quân A",
                text = "Rất tốt. Toàn quân nghe lệnh: Chú ý đội hình, chuẩn bị nghênh chiến bảo vệ hoàng thành!",
                isCharacterA = true
            });
        }

        // Setup portraits
        if (leftPortrait != null && characterASprite != null) leftPortrait.sprite = characterASprite;
        if (rightPortrait != null && characterBSprite != null) rightPortrait.sprite = characterBSprite;

        // Add click listener to the fullscreen overlay to advance text
        Button overlayBtn = dialogueOverlay.GetComponent<Button>();
        if (overlayBtn != null)
        {
            overlayBtn.onClick.AddListener(OnOverlayClicked);
        }

        // Start the dialogue sequence
        StartDialogue();
    }

    void Update()
    {
        // Smoothly lerp portraits scale and color for highlight effect
        if (leftPortrait != null)
        {
            leftPortrait.transform.localScale = Vector3.Lerp(leftPortrait.transform.localScale, leftTargetScale, Time.deltaTime * transitionSpeed);
            leftPortrait.color = Color.Lerp(leftPortrait.color, leftTargetColor, Time.deltaTime * transitionSpeed);
        }

        if (rightPortrait != null)
        {
            rightPortrait.transform.localScale = Vector3.Lerp(rightPortrait.transform.localScale, rightTargetScale, Time.deltaTime * transitionSpeed);
            rightPortrait.color = Color.Lerp(rightPortrait.color, rightTargetColor, Time.deltaTime * transitionSpeed);
        }
    }

    public System.Action onDialogueStart;
    public System.Action onDialogueEnd;

    public void StartDialogue()
    {
        if (dialogueOverlay != null)
        {
            dialogueOverlay.SetActive(true);
        }

        // Invoke start callback (hides background panels)
        if (onDialogueStart != null)
        {
            onDialogueStart.Invoke();
        }

        // Lock camera control and input
        if (mapCamera != null)
        {
            mapCamera.enabled = false;
        }

        currentNodeIndex = 0;
        DisplayCurrentNode();
    }

    private void OnOverlayClicked()
    {
        if (isTyping)
        {
            // Skip typing and show full text immediately
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = fullTextToShow;
            isTyping = false;
        }
        else
        {
            // Advance to next node
            currentNodeIndex++;
            if (currentNodeIndex < dialogueNodes.Count)
            {
                DisplayCurrentNode();
            }
            else
            {
                EndDialogue();
            }
        }
    }

    private void DisplayCurrentNode()
    {
        if (currentNodeIndex < 0 || currentNodeIndex >= dialogueNodes.Count) return;

        DialogueNode node = dialogueNodes[currentNodeIndex];
        
        nameText.text = node.speakerName;
        fullTextToShow = node.text;

        // Highlight speaker portrait, dim the listener
        if (node.isCharacterA)
        {
            leftTargetScale = new Vector3(2.1f, 2.1f, 2.1f); // To gấp 2 lần (1.05 * 2)
            leftTargetColor = Color.white;

            rightTargetScale = new Vector3(1.84f, 1.84f, 1.84f); // To gấp 2 lần (0.92 * 2)
            rightTargetColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);

            nameText.alignment = TextAnchor.MiddleLeft;
        }
        else
        {
            leftTargetScale = new Vector3(1.84f, 1.84f, 1.84f);
            leftTargetColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);

            rightTargetScale = new Vector3(2.1f, 2.1f, 2.1f);
            rightTargetColor = Color.white;

            nameText.alignment = TextAnchor.MiddleRight;
        }

        // Start typewriter text animation
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(fullTextToShow));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        
        isTyping = false;
    }

    private void EndDialogue()
    {
        if (dialogueOverlay != null)
        {
            dialogueOverlay.SetActive(false);
        }

        // Invoke end callback (restores background panels)
        if (onDialogueEnd != null)
        {
            onDialogueEnd.Invoke();
        }

        // Re-enable camera controls
        if (mapCamera != null)
        {
            mapCamera.enabled = true;
        }

        Debug.Log("[DialogueSystem] Dialogue ended. Interactions unlocked.");
    }
}
