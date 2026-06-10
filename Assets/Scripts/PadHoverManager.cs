using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Attached to a central manager. Every frame, raycasts from mouse to find pads
/// and highlights the hovered one with a glow effect.
/// </summary>
public class PadHoverManager : MonoBehaviour
{
    [Header("Hover Settings")]
    public Color hoverColor = new Color(1f, 1f, 0.5f, 1f); // bright yellow glow
    public float glowIntensity = 2.0f;

    private Camera _cam;
    private GameObject _lastHoveredPad;
    private Material _lastOriginalMat;
    private Material _hoverMat;

    void Start()
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            _cam = Object.FindAnyObjectByType<Camera>();
        }

        // Create the hover glow material
        _hoverMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (_hoverMat != null)
        {
            _hoverMat.color = hoverColor;
            if (_hoverMat.HasProperty("_BaseColor"))
            {
                _hoverMat.SetColor("_BaseColor", hoverColor * glowIntensity);
            }
            _hoverMat.SetFloat("_Surface", 1); // Transparent
            _hoverMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _hoverMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _hoverMat.SetInt("_ZWrite", 0);
            _hoverMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _hoverMat.SetOverrideTag("RenderType", "Transparent");
            _hoverMat.renderQueue = 3000;
        }
    }

    void Update()
    {
        if (_cam == null) return;

        // Get mouse position
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
#else
        Vector2 mousePos = Input.mousePosition;
#endif

        // Raycast for pads using RaycastAll
        Ray ray = _cam.ScreenPointToRay(mousePos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, Physics.AllLayers, QueryTriggerInteraction.Collide);

        GameObject hoveredPad = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.collider != null &&
                (hit.collider.name.StartsWith("PlayerPad_") || hit.collider.name.StartsWith("Tile_")))
            {
                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    hoveredPad = hit.collider.gameObject;
                }
            }
        }

        // If we're hovering a different pad than before, restore the old one
        if (hoveredPad != _lastHoveredPad)
        {
            // Restore previous pad
            if (_lastHoveredPad != null && _lastOriginalMat != null)
            {
                Renderer lastRend = _lastHoveredPad.GetComponent<Renderer>();
                if (lastRend != null)
                {
                    lastRend.sharedMaterial = _lastOriginalMat;
                }
            }

            _lastHoveredPad = hoveredPad;
            _lastOriginalMat = null;

            // Highlight new pad
            if (_lastHoveredPad != null)
            {
                Renderer rend = _lastHoveredPad.GetComponent<Renderer>();
                if (rend != null)
                {
                    _lastOriginalMat = rend.sharedMaterial;
                    rend.sharedMaterial = _hoverMat;
                }

                Debug.Log("Hovering pad: " + _lastHoveredPad.name);
            }
        }
    }

    void OnDisable()
    {
        // Restore the last hovered pad on disable
        if (_lastHoveredPad != null && _lastOriginalMat != null)
        {
            Renderer rend = _lastHoveredPad.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = _lastOriginalMat;
            }
        }
        _lastHoveredPad = null;
        _lastOriginalMat = null;
    }
}
