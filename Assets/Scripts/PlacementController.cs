using UnityEngine;

public class PlacementController : MonoBehaviour
{
    public static PlacementController Instance { get; private set; }

    [HideInInspector]
    public DragDropCard selectedCard;

    private Camera _cam;
    private GameObject _clickPreviewCapsule;
    private Unit _draggedUnit;
    private Vector3 _draggedUnitOriginalPos;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            _cam = Object.FindAnyObjectByType<Camera>();
        }
    }

    void OnDisable()
    {
        if (_clickPreviewCapsule != null)
        {
            Destroy(_clickPreviewCapsule);
            _clickPreviewCapsule = null;
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
                    bestDist = hit.distance;
                    bestPad = hit.collider.gameObject;
                }
            }
        }

        return bestPad;
    }

    /// <summary>
    /// Returns a world position for the preview capsule — snaps to pad if found, else projects to ground.
    /// </summary>
    private bool GetPreviewPosition(Vector2 screenPos, out Vector3 worldPos)
    {
        worldPos = Vector3.zero;
        if (_cam == null) return false;

        // First try to snap to a pad
        GameObject pad = RaycastForPad(screenPos);
        if (pad != null)
        {
            worldPos = pad.transform.position;
            return true;
        }

        // Fall back to ground plane
        Ray ray = _cam.ScreenPointToRay(screenPos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }

        return false;
    }

    private Vector2 GetMouseScreenPos()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
            return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        return Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    private bool WasLeftClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return UnityEngine.InputSystem.Mouse.current != null &&
               UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Placement)
        {
            if (_clickPreviewCapsule != null)
            {
                Destroy(_clickPreviewCapsule);
                _clickPreviewCapsule = null;
            }
            return;
        }

        bool isPlayer = true;
        if (CameraController.Instance != null && CameraController.Instance.GetCurrentView() == CameraView.EnemySetup)
        {
            isPlayer = false;
        }

        if (!isPlayer)
        {
            if (_clickPreviewCapsule != null)
            {
                Destroy(_clickPreviewCapsule);
                _clickPreviewCapsule = null;
            }
            return; // Do not show placement previews or allow placing on enemy setup view at runtime
        }

        // Click-to-place logic & preview shadow management
        if (selectedCard != null)
        {
            // Spawn preview capsule if it doesn't exist
            if (_clickPreviewCapsule == null)
            {
                _clickPreviewCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _clickPreviewCapsule.name = "ClickPlacementPreviewCapsule";
                
                var col = _clickPreviewCapsule.GetComponent<Collider>();
                if (col != null) Destroy(col);
                
                var rend = _clickPreviewCapsule.GetComponent<Renderer>();
                if (rend != null)
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
                        rend.sharedMaterial = previewMat;
                    }
                    else
                    {
                        rend.material.color = new Color(0.2f, 0.6f, 1f, 0.5f);
                    }
                }
                float capScale = 15f;
                if (GameManager.Instance != null)
                {
                    capScale = GameManager.Instance.capsuleScale;
                }
                _clickPreviewCapsule.transform.localScale = new Vector3(capScale * 0.8f, capScale * 0.8f, capScale * 0.8f);
            }

            // Update preview position
            Vector2 mousePos = GetMouseScreenPos();
            if (GetPreviewPosition(mousePos, out Vector3 previewWorldPos))
            {
                float capScale = 15f;
                if (GameManager.Instance != null)
                {
                    capScale = GameManager.Instance.capsuleScale;
                }
                _clickPreviewCapsule.transform.position = previewWorldPos + Vector3.up * capScale;
                _clickPreviewCapsule.SetActive(true);
            }
            else
            {
                _clickPreviewCapsule.SetActive(false);
            }

            // Perform placement on mouse click
            if (WasLeftClickPressed())
            {
                // If pointer is over UI element, do not treat it as a placement click
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                GameObject padHit = RaycastForPad(mousePos);
                if (padHit != null)
                {
                    AttemptPlacement(padHit);
                }
            }
        }
        else
        {
            // Destroy preview if no card is selected
            if (_clickPreviewCapsule != null)
            {
                Destroy(_clickPreviewCapsule);
                _clickPreviewCapsule = null;
            }
        }

        // Click and drag placed units logic
        if (WasLeftClickPressed())
        {
            if (UnityEngine.EventSystems.EventSystem.current == null ||
                !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                Ray ray = _cam.ScreenPointToRay(GetMouseScreenPos());
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    Unit clickedUnit = hit.collider.GetComponentInParent<Unit>();
                    if (clickedUnit != null && clickedUnit.isPlayer)
                    {
                        if (selectedCard != null)
                        {
                            selectedCard.SetSelected(false);
                        }

                        _draggedUnit = clickedUnit;
                        _draggedUnitOriginalPos = _draggedUnit.transform.position;
                        Debug.Log("Started dragging unit: " + _draggedUnit.name);
                    }
                }
            }
        }

        if (_draggedUnit != null)
        {
            if (IsLeftClickHeld())
            {
                Vector2 mousePos = GetMouseScreenPos();
                if (GetPreviewPosition(mousePos, out Vector3 targetWorldPos))
                {
                    GameObject padHit = RaycastForPad(mousePos);
                    if (padHit != null)
                    {
                        if (!IsTileOccupied(padHit.transform.position, true, _draggedUnit))
                        {
                            _draggedUnit.transform.position = padHit.transform.position;
                        }
                        else
                        {
                            _draggedUnit.transform.position = targetWorldPos;
                        }
                    }
                    else
                    {
                        _draggedUnit.transform.position = targetWorldPos;
                    }
                }
            }
            else
            {
                Vector2 mousePos = GetMouseScreenPos();

                if (IsPointerOverBottomPanel(mousePos))
                {
                    Debug.Log("Unit dragged to card panel. Removing unit: " + _draggedUnit.name);
                    GameManager.Instance.playerUnits.Remove(_draggedUnit);
                    GameManager.Instance.placedPlayerUnits = Mathf.Max(0, GameManager.Instance.placedPlayerUnits - 1);

                    Destroy(_draggedUnit.gameObject);
                    _draggedUnit = null;

                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.UpdatePlacementUI();
                    }
                }
                else
                {
                    GameObject padHit = RaycastForPad(mousePos);
                    if (padHit != null && !IsTileOccupied(padHit.transform.position, true, _draggedUnit))
                    {
                        _draggedUnit.transform.position = padHit.transform.position;
                        Debug.Log("Successfully moved unit to: " + padHit.name);
                        _draggedUnit = null;
                    }
                    else
                    {
                        _draggedUnit.transform.position = _draggedUnitOriginalPos;
                        Debug.Log("Invalid placement. Returned unit to original position.");
                        _draggedUnit = null;
                    }
                }
            }
        }
    }

    public void AttemptPlacement(GameObject tileObj)
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Placement)
            return;

        if (tileObj == null || tileObj.name.StartsWith("EnemyPad"))
        {
            Debug.LogWarning("PlacementController: Placement on EnemyPad is not allowed at runtime!");
            return;
        }

        bool isPlayer = true;
        if (CameraController.Instance != null && CameraController.Instance.GetCurrentView() == CameraView.EnemySetup)
        {
            isPlayer = false;
        }

        // Do not allow placing units on enemy side at runtime (only via Level Editor)
        if (!isPlayer) return;

        int maxUnits = isPlayer ? GameManager.Instance.maxPlayerUnits : GameManager.Instance.maxEnemyUnits;
        int placedUnits = isPlayer ? GameManager.Instance.placedPlayerUnits : GameManager.Instance.placedEnemyUnits;

        if (placedUnits >= maxUnits)
            return;

        string prefix = isPlayer ? "PlayerPad_" : "EnemyPad_";
            
        if (tileObj.name.StartsWith(prefix) || tileObj.name.StartsWith("Tile_"))
        {
            // Check if tile is empty
            if (!IsTileOccupied(tileObj.transform.position, isPlayer))
            {
                GameManager.Instance.SpawnUnit(isPlayer, tileObj.transform.position);
                if (isPlayer)
                {
                    GameManager.Instance.placedPlayerUnits++;
                }
                else
                {
                    GameManager.Instance.placedEnemyUnits++;
                }

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdatePlacementUI();
                }

                // If placed via click selection, clear the selection
                if (selectedCard != null)
                {
                    selectedCard.SetSelected(false);
                }
            }
            else
            {
                Debug.Log("Tile already occupied!");
            }
        }
    }

    private bool IsLeftClickHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return UnityEngine.InputSystem.Mouse.current != null &&
               UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    private bool IsPointerOverBottomPanel(Vector2 screenPos)
    {
        if (UIManager.Instance == null || UIManager.Instance.bottomPanel == null) return false;
        RectTransform rect = UIManager.Instance.bottomPanel.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null);
    }

    public bool IsTileOccupied(Vector3 position, bool checkPlayer, Unit ignoreUnit = null)
    {
        var units = checkPlayer ? GameManager.Instance.playerUnits : GameManager.Instance.enemyUnits;
        foreach (var unit in units)
        {
            if (unit == ignoreUnit) continue;
            Vector3 unitPos = unit.transform.position;
            unitPos.y = position.y; // ignore height difference
            if (Vector3.Distance(unitPos, position) < 0.5f)
            {
                return true;
            }
        }
        return false;
    }
}
