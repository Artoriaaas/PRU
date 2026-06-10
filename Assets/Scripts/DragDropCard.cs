using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDropCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private RectTransform _dragIcon;
    private Canvas _canvas;
    private Image _image;
    private Color _originalColor;
    private bool _isSelected = false;
    private GameObject _previewCapsule;
    private Camera _cam;

    void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        _image = GetComponent<Image>();
        if (_image != null)
        {
            _originalColor = _image.color;
        }

        _cam = Camera.main;
        if (_cam == null)
        {
            _cam = Object.FindAnyObjectByType<Camera>();
        }
    }

    /// <summary>
    /// Fires a ray and returns the first PlayerPad/Tile hit, ignoring the ground plane.
    /// Uses RaycastAll to find trigger colliders that sit on top of solid ground.
    /// </summary>
    private GameObject RaycastForPad(Vector2 screenPos)
    {
        if (_cam == null) return null;

        Ray ray = _cam.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, Physics.AllLayers, QueryTriggerInteraction.Collide);

        // Prioritize pad hits over anything else
        GameObject bestPad = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.collider != null &&
                (hit.collider.name.StartsWith("PlayerPad_") || hit.collider.name.StartsWith("Tile_")))
            {
                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    bestPad = hit.collider.gameObject;
                }
            }
        }

        return bestPad;
    }

    /// <summary>
    /// Fires a ray and returns a world position on the ground plane (Y=0) for preview positioning.
    /// </summary>
    private bool RaycastForWorldPos(Vector2 screenPos, out Vector3 worldPos)
    {
        worldPos = Vector3.zero;
        if (_cam == null) return false;

        Ray ray = _cam.ScreenPointToRay(screenPos);

        // First, try to find a pad to snap to
        GameObject pad = RaycastForPad(screenPos);
        if (pad != null)
        {
            worldPos = pad.transform.position;
            return true;
        }

        // Fall back to ground plane projection
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }

        return false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Placement) return;
        if (GameManager.Instance.placedPlayerUnits >= GameManager.Instance.maxPlayerUnits) return;

        // Clear click selection if we drag
        SetSelected(false);

        // Create a temporary icon to follow mouse
        GameObject iconObj = new GameObject("DragIcon");
        iconObj.transform.SetParent(_canvas.transform, false);
        Image img = iconObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 1f, 0.7f); // semi-transparent blue
        _dragIcon = iconObj.GetComponent<RectTransform>();
        _dragIcon.sizeDelta = new Vector2(50, 50);

        // Create capsule preview shadow
        _previewCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        _previewCapsule.name = "DragPlacementPreviewCapsule";
        var previewCol = _previewCapsule.GetComponent<Collider>();
        if (previewCol != null) Destroy(previewCol);
        
        var previewRend = _previewCapsule.GetComponent<Renderer>();
        if (previewRend != null)
        {
            Material previewMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (previewMat != null)
            {
                previewMat.color = new Color(0.2f, 0.6f, 1f, 0.5f);
                previewMat.SetFloat("_Surface", 1); // Transparent
                previewMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                previewMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                previewMat.SetInt("_ZWrite", 0);
                previewMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                previewRend.sharedMaterial = previewMat;
            }
            else
            {
                previewRend.material.color = new Color(0.2f, 0.6f, 1f, 0.5f);
            }
        }
        float capScale = 15f;
        if (GameManager.Instance != null)
        {
            capScale = GameManager.Instance.capsuleScale;
        }
        _previewCapsule.transform.localScale = new Vector3(capScale * 0.8f, capScale * 0.8f, capScale * 0.8f);
        _previewCapsule.SetActive(false);

        UpdateDragPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragIcon != null)
        {
            UpdateDragPosition(eventData);
        }

        if (_previewCapsule != null)
        {
            if (RaycastForWorldPos(eventData.position, out Vector3 worldPos))
            {
                float capScale = 15f;
                if (GameManager.Instance != null)
                {
                    capScale = GameManager.Instance.capsuleScale;
                }
                _previewCapsule.transform.position = worldPos + Vector3.up * capScale;
                _previewCapsule.SetActive(true);
            }
            else
            {
                _previewCapsule.SetActive(false);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_previewCapsule != null)
        {
            Destroy(_previewCapsule);
            _previewCapsule = null;
        }

        if (_dragIcon != null)
        {
            Destroy(_dragIcon.gameObject);
            _dragIcon = null;

            // Check if dropped on a pad
            GameObject padHit = RaycastForPad(eventData.position);
            if (padHit != null)
            {
                PlacementController pc = PlacementController.Instance;
                if (pc != null)
                {
                    pc.AttemptPlacement(padHit);
                }
                else
                {
                    Debug.LogWarning("PlacementController not found in scene!");
                }
            }
            else
            {
                Debug.Log("Drag ended but no pad was hit.");
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Don't register click if we were dragging
        if (eventData.dragging) return;

        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Placement) return;
        if (GameManager.Instance.placedPlayerUnits >= GameManager.Instance.maxPlayerUnits) return;

        ToggleSelection();
    }

    public void ToggleSelection()
    {
        SetSelected(!_isSelected);
    }

    public void SetSelected(bool select)
    {
        _isSelected = select;
        if (_image != null)
        {
            // Highlight selected card with a gold/orange color
            _image.color = _isSelected ? new Color(1f, 0.75f, 0.1f, 1f) : _originalColor;
        }

        if (PlacementController.Instance != null)
        {
            if (_isSelected)
            {
                // Unselect all other cards
                DragDropCard[] allCards = FindObjectsByType<DragDropCard>(FindObjectsInactive.Exclude);
                foreach (var card in allCards)
                {
                    if (card != this)
                    {
                        card.SetSelected(false);
                    }
                }
                PlacementController.Instance.selectedCard = this;
            }
            else
            {
                if (PlacementController.Instance.selectedCard == this)
                {
                    PlacementController.Instance.selectedCard = null;
                }
            }
        }
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        Vector2 localPointerPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(), 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPointerPosition);
        _dragIcon.localPosition = localPointerPosition;
    }

    void OnDisable()
    {
        if (_previewCapsule != null)
        {
            Destroy(_previewCapsule);
            _previewCapsule = null;
        }
    }
}
