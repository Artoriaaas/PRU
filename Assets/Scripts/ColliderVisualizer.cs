using UnityEngine;

public class ColliderVisualizer : MonoBehaviour
{
    public static bool ShowColliders = false;

    private CapsuleCollider _collider;
    private GameObject _visualObj;
    private GameObject _directionIndicator;

    void Start()
    {
        _collider = GetComponent<CapsuleCollider>();
    }

    void Update()
    {
        if (ShowColliders && _visualObj == null)
        {
            CreateVisual();
        }
        else if (!ShowColliders && _visualObj != null)
        {
            DestroyVisual();
        }

        if (_visualObj != null && _collider != null)
        {
            SyncTransform();
        }
    }

    private void CreateVisual()
    {
        if (_collider == null) _collider = GetComponent<CapsuleCollider>();
        if (_collider == null) return;

        // 1. Create Capsule Hitbox Visual
        _visualObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        _visualObj.name = "ColliderVisualOverride";

        var col = _visualObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Determine if player or enemy unit
        Unit unit = GetComponent<Unit>();
        bool isPlayerUnit = unit == null || unit.isPlayer;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null || shader.name == "Hidden/InternalErrorShader")
        {
            shader = Shader.Find("Standard");
        }
        if (shader == null || shader.name == "Hidden/InternalErrorShader")
        {
            shader = Shader.Find("Sprites/Default");
        }

        var rend = _visualObj.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(shader);
            mat.color = isPlayerUnit ? new Color(0.2f, 1f, 0.2f, 0.2f) : new Color(1f, 0.2f, 0.2f, 0.2f);
            SetupTransparency(mat);
            rend.material = mat;
        }

        _visualObj.transform.SetParent(transform, false);

        // 2. Create Forward Direction Indicator (Thin Cylinder pointing forward)
        _directionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _directionIndicator.name = "ColliderDirectionIndicator";

        var indCol = _directionIndicator.GetComponent<Collider>();
        if (indCol != null) Destroy(indCol);

        var indRend = _directionIndicator.GetComponent<Renderer>();
        if (indRend != null)
        {
            Material indMat = new Material(shader);
            // Higher alpha (0.7f) so the forward arrow is clearly visible
            indMat.color = isPlayerUnit ? new Color(0.2f, 1f, 0.2f, 0.7f) : new Color(1f, 0.2f, 0.2f, 0.7f);
            SetupTransparency(indMat);
            indRend.material = indMat;
        }

        _directionIndicator.transform.SetParent(transform, false);

        SyncTransform();
    }

    private void SetupTransparency(Material mat)
    {
        if (mat == null) return;

        if (mat.shader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetFloat("_Surface", 1f); // 1 = Transparent
            mat.SetFloat("_Blend", 0f); // 0 = Alpha blend
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_BLENDMODE_ALPHA");
        }
        else if (mat.shader.name.Contains("Standard"))
        {
            mat.SetFloat("_Mode", 3f); // 3 = Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; // 3000
    }

    private void DestroyVisual()
    {
        if (_visualObj != null)
        {
            Destroy(_visualObj);
            _visualObj = null;
        }
        if (_directionIndicator != null)
        {
            Destroy(_directionIndicator);
            _directionIndicator = null;
        }
    }

    private void SyncTransform()
    {
        if (_collider == null) return;

        if (_visualObj != null)
        {
            _visualObj.transform.localPosition = _collider.center;
            _visualObj.transform.localScale = new Vector3(_collider.radius * 2f, _collider.height / 2f, _collider.radius * 2f);
            _visualObj.transform.localRotation = Quaternion.identity;
        }

        if (_directionIndicator != null)
        {
            Vector3 center = _collider.center;
            float radius = _collider.radius;
            float indicatorLength = radius * 2.5f;
            float thickness = radius * 0.15f;

            // Offset the cylinder so its base sits at the center, extending forward
            _directionIndicator.transform.localPosition = center + Vector3.forward * (indicatorLength / 2f);
            
            // Cylinders default to vertical (Y-axis aligned). Rotate X by 90 to make it face forward (Z-axis)
            _directionIndicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            
            // X & Z are diameter/thickness, Y is height/2 (since standard height is 2)
            _directionIndicator.transform.localScale = new Vector3(thickness, indicatorLength / 2f, thickness);
        }
    }

    void OnDisable()
    {
        DestroyVisual();
    }
}
