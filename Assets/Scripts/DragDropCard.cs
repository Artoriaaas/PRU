using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDropCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private Canvas _canvas;
    private Image _image;
    private Color _originalColor;
    private bool _isSelected = false;
    private GameObject _previewCapsule;
    private Camera _cam;
    public int unitTypeIndex = 0;

    private Color GetColorForType(int typeIndex, float alpha = 0.5f)
    {
        if (typeIndex == 0) return new Color(0.1f, 0.4f, 0.8f, alpha);
        if (typeIndex == 1) return new Color(0.1f, 0.6f, 0.2f, alpha);
        if (typeIndex == 2) return new Color(0.5f, 0.1f, 0.7f, alpha);
        if (typeIndex == 3) return new Color(0.85f, 0.5f, 0.1f, alpha);
        if (typeIndex == 4) return new Color(0.5f, 0.1f, 0.7f, alpha); // Purple for General/King
        return new Color(0.2f, 0.6f, 1f, alpha);
    }

    void Start()
    {
        // Force General card (index 2) to 4 (King) so it matches logic in scene
        if (unitTypeIndex == 2)
        {
            unitTypeIndex = 4;
        }

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

        string prefix = "PlayerPad_";

        foreach (var hit in hits)
        {
            if (hit.collider != null &&
                (hit.collider.name.StartsWith(prefix) || hit.collider.name.StartsWith("Tile_")))
            {
                if (hit.distance < bestDist)
                {
                    // Enforce King vs other unit pad restrictions
                    if (hit.collider.name == "PlayerPad_3_2 (1)")
                    {
                        if (unitTypeIndex != 4) continue;
                    }
                    else
                    {
                        if (unitTypeIndex == 4) continue;
                    }

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

        bool isPlayer = true;
        if (CameraController.Instance != null && CameraController.Instance.GetCurrentView() == CameraView.EnemySetup)
        {
            isPlayer = false;
        }

        // Do not allow dragging/placing enemy units at runtime (only via Level Editor)
        if (!isPlayer) return;

        int maxUnits = isPlayer ? GameManager.Instance.maxPlayerUnits : GameManager.Instance.maxEnemyUnits;
        int placedUnits = isPlayer ? GameManager.Instance.placedPlayerUnits : GameManager.Instance.placedEnemyUnits;

        if (placedUnits >= maxUnits) return;

        // Clear click selection if we drag
        SetSelected(false);

        // Color scheme depending on player vs enemy and type index
        Color previewColor = isPlayer ? GetColorForType(unitTypeIndex, 0.5f) : new Color(1f, 0.2f, 0.2f, 0.5f);

        // Create capsule preview shadow
        _previewCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        _previewCapsule.name = "DragPlacementPreviewCapsule";
        var previewCol = _previewCapsule.GetComponent<Collider>();
        if (previewCol != null) Destroy(previewCol);
        
        var previewRend = _previewCapsule.GetComponent<Renderer>();
        if (previewRend != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null || shader.name == "Hidden/InternalErrorShader")
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null || shader.name == "Hidden/InternalErrorShader")
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material previewMat = new Material(shader);
            previewMat.color = previewColor;

            // Configure transparent rendering settings based on shader type
            if (previewMat.shader.name.Contains("Universal Render Pipeline"))
            {
                previewMat.SetFloat("_Surface", 1f); // 1 = Transparent
                previewMat.SetFloat("_Blend", 0f); // 0 = Alpha blend
                previewMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                previewMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                previewMat.SetInt("_ZWrite", 0);
                previewMat.DisableKeyword("_ALPHATEST_ON");
                previewMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                previewMat.EnableKeyword("_BLENDMODE_ALPHA");
            }
            else if (previewMat.shader.name.Contains("Standard"))
            {
                previewMat.SetFloat("_Mode", 3f); // 3 = Transparent
                previewMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                previewMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                previewMat.SetInt("_ZWrite", 0);
                previewMat.DisableKeyword("_ALPHATEST_ON");
                previewMat.EnableKeyword("_ALPHABLEND_ON");
                previewMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            previewMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; // 3000
            previewRend.sharedMaterial = previewMat;
        }
        float capScale = 15f;
        if (GameManager.Instance != null)
        {
            capScale = GameManager.Instance.capsuleScale;
        }
        _previewCapsule.transform.localScale = new Vector3(capScale * 0.8f, capScale * 0.8f, capScale * 0.8f);
        _previewCapsule.SetActive(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
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

        // Check if dropped on a pad
        GameObject padHit = RaycastForPad(eventData.position);
        if (padHit != null)
        {
            PlacementController pc = PlacementController.Instance;
            if (pc != null)
            {
                pc.AttemptPlacement(padHit, unitTypeIndex);
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

    public void OnPointerClick(PointerEventData eventData)
    {
        // Don't register click if we were dragging
        if (eventData.dragging) return;

        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Placement) return;

        bool isPlayer = true;
        if (CameraController.Instance != null && CameraController.Instance.GetCurrentView() == CameraView.EnemySetup)
        {
            isPlayer = false;
        }

        // Do not allow card selection/clicking when in enemy setup view
        if (!isPlayer) return;

        int maxUnits = isPlayer ? GameManager.Instance.maxPlayerUnits : GameManager.Instance.maxEnemyUnits;
        int placedUnits = isPlayer ? GameManager.Instance.placedPlayerUnits : GameManager.Instance.placedEnemyUnits;

        if (placedUnits >= maxUnits) return;

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



    void OnDisable()
    {
        if (_previewCapsule != null)
        {
            Destroy(_previewCapsule);
            _previewCapsule = null;
        }
    }
}
