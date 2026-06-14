using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Attached to a central manager. Every frame, raycasts from mouse to find pads
/// and highlights the hovered one with a glow effect.
/// Uses a smooth scale transition and a raycast miss buffer to avoid PhysX static collider jitter.
/// </summary>
public class PadHoverManager : MonoBehaviour
{
    [Header("Hover Settings")]
    public Color hoverColor = new Color(1f, 0.9f, 0.3f, 0.8f); // premium gold/yellow glow
    public float glowIntensity = 1.5f;
    public Texture2D hoverTexture;

    private Camera _cam;
    private GameObject _lastHoveredPad;
    private Material _hoverMat;

    // Track original scales and materials to animate and restore them smoothly
    private Dictionary<GameObject, Vector3> _originalScales = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, float> _scaleProgresses = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, Material> _originalMaterials = new Dictionary<GameObject, Material>();

    private int _missFrames = 0;
    private const int MAX_MISS_FRAMES = 4; // Buffer to prevent single-frame raycast dropouts

    void Start()
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            _cam = Object.FindAnyObjectByType<Camera>();
        }

        // Try to load the texture in editor if not assigned
        if (hoverTexture == null)
        {
#if UNITY_EDITOR
            hoverTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/SelectedNodeRe.png");
#endif
        }

        // Create the hover glow material using URP Unlit shader
        _hoverMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (_hoverMat != null)
        {
            _hoverMat.color = hoverColor;
            if (hoverTexture != null)
            {
                _hoverMat.SetTexture("_BaseMap", hoverTexture);
                _hoverMat.SetTexture("_MainTex", hoverTexture);
            }
            if (_hoverMat.HasProperty("_BaseColor"))
            {
                _hoverMat.SetColor("_BaseColor", hoverColor * glowIntensity);
            }
            
            // Set as transparent alpha blended
            _hoverMat.SetFloat("_Surface", 1); // Transparent
            _hoverMat.SetFloat("_Blend", 0); // Alpha
            _hoverMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _hoverMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _hoverMat.SetInt("_ZWrite", 0);
            _hoverMat.DisableKeyword("_ALPHATEST_ON");
            _hoverMat.EnableKeyword("_ALPHABLEND_ON");
            _hoverMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
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

        // Always check only for PlayerPad_ at runtime (only allow player-side interaction)
        string prefix = "PlayerPad_";

        foreach (var hit in hits)
        {
            if (hit.collider != null &&
                (hit.collider.name.StartsWith(prefix) || hit.collider.name.StartsWith("Tile_")))
            {
                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    hoveredPad = hit.collider.gameObject;
                }
            }
        }

        // Apply frame buffer to smooth out temporary raycast misses
        if (hoveredPad != null)
        {
            _missFrames = 0;
        }
        else if (_lastHoveredPad != null)
        {
            _missFrames++;
            if (_missFrames < MAX_MISS_FRAMES)
            {
                hoveredPad = _lastHoveredPad;
            }
        }

        // Handle hover target changes
        if (hoveredPad != _lastHoveredPad)
        {
            _lastHoveredPad = hoveredPad;

            if (_lastHoveredPad != null)
            {
                // Register original scale if not tracked yet
                if (!_originalScales.ContainsKey(_lastHoveredPad))
                {
                    _originalScales[_lastHoveredPad] = _lastHoveredPad.transform.localScale;
                }

                // Swap material to hover highlight and track original
                Renderer rend = _lastHoveredPad.GetComponent<Renderer>();
                if (rend != null && !_originalMaterials.ContainsKey(_lastHoveredPad))
                {
                    _originalMaterials[_lastHoveredPad] = rend.sharedMaterial;
                    rend.sharedMaterial = _hoverMat;
                }
            }
        }

        // Animate the hover material alpha/glow
        if (_hoverMat != null)
        {
            float alphaPulse = 0.5f + Mathf.PingPong(Time.time * 2f, 0.4f); // oscillates alpha between 0.5 and 0.9
            Color pulseColor = hoverColor;
            pulseColor.a = alphaPulse;
            _hoverMat.color = pulseColor;
            if (_hoverMat.HasProperty("_BaseColor"))
            {
                _hoverMat.SetColor("_BaseColor", pulseColor * glowIntensity);
            }
        }

        // Smoothly scale all tracked pads and restore them when done
        List<GameObject> keys = new List<GameObject>(_originalScales.Keys);
        foreach (var pad in keys)
        {
            if (pad == null)
            {
                _originalScales.Remove(pad);
                _scaleProgresses.Remove(pad);
                _originalMaterials.Remove(pad);
                continue;
            }

            bool isCurrent = (pad == _lastHoveredPad);
            float currentProgress = _scaleProgresses.ContainsKey(pad) ? _scaleProgresses[pad] : 0f;
            float targetProgress = isCurrent ? 1f : 0f;

            // Increment scale progress smoothly
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * 6f);
            _scaleProgresses[pad] = currentProgress;

            Vector3 origScale = _originalScales[pad];
            pad.transform.localScale = Vector3.Lerp(origScale, origScale * 1.08f, currentProgress);

            // Once fully scaled down, restore material and remove from lists to prevent frame-by-frame updates
            if (!isCurrent && currentProgress <= 0f)
            {
                Renderer rend = pad.GetComponent<Renderer>();
                if (rend != null && _originalMaterials.ContainsKey(pad))
                {
                    rend.sharedMaterial = _originalMaterials[pad];
                }
                pad.transform.localScale = origScale;

                _originalScales.Remove(pad);
                _scaleProgresses.Remove(pad);
                _originalMaterials.Remove(pad);
            }
        }
    }

    void OnDisable()
    {
        // Force restore all remaining tracked pads
        foreach (var pair in _originalScales)
        {
            GameObject pad = pair.Key;
            if (pad != null)
            {
                pad.transform.localScale = pair.Value;
                Renderer rend = pad.GetComponent<Renderer>();
                if (rend != null && _originalMaterials.ContainsKey(pad))
                {
                    rend.sharedMaterial = _originalMaterials[pad];
                }
            }
        }
        _originalScales.Clear();
        _scaleProgresses.Clear();
        _originalMaterials.Clear();
        _lastHoveredPad = null;
    }
}


