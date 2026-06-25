using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    private Unit _target;
    private float _damage;
    private float _speed;
    private float _arcHeight;
    
    private Vector3 _startPos;
    private Vector3 _targetPos;
    private float _duration;
    private float _elapsedTime;
    private float _yOffset;
    private bool _isInitialized = false;

    public void Initialize(Unit target, float damage, float speed, float arcHeight, float scale)
    {
        _target = target;
        _damage = damage;
        _speed = speed;
        _speed = speed;
        if (_speed < 10f) _speed = 200f;

        float targetHeight = 1.5f * (scale / 60f);
        if (_target != null)
        {
            CapsuleCollider col = _target.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                targetHeight = col.height;
            }
        }
        // Target upper body/head (55% to 75% of target height)
        _yOffset = targetHeight * Random.Range(0.55f, 0.75f);

        _startPos = transform.position;
        if (_target != null)
        {
            _targetPos = _target.transform.position + Vector3.up * _yOffset;
        }
        else
        {
            _targetPos = _startPos + transform.forward * 10f * scale;
        }

        float distance = Vector3.Distance(_startPos, _targetPos);
        
        // Scale arc height based on distance (8% of travel distance) to keep close shots flat
        float maxArc = arcHeight >= 1f ? arcHeight : 15f;
        _arcHeight = Mathf.Clamp(distance * 0.08f, 0.5f * (scale / 60f), maxArc);
        _duration = distance / _speed;
        
        // Ultimate safety check to prevent frozen arrows due to invalid math (NaN, Infinity, or 0)
        if (float.IsNaN(_duration) || float.IsInfinity(_duration) || _duration < 0.05f)
        {
            _duration = 1.0f; 
        }
        _elapsedTime = 0f;

        Debug.Log($"[ArrowDebug] ArrowProjectile {name} Initialized: start={_startPos}, targetPos={_targetPos}, dist={distance}, duration={_duration}, speed={_speed}");

        // Configure TrailRenderer programmatically for the white streak effect
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
        }

        trail.time = 0.25f;
        trail.startWidth = 0.12f * (scale / 60f) * 15f;
        trail.endWidth = 0.005f * (scale / 60f) * 15f;
        trail.autodestruct = false;

        // Pure white trail gradient fading out to transparent
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        trail.colorGradient = gradient;

        // Find a compatible material shader (URP or standard Sprites/Default)
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
        if (shader == null) shader = Shader.Find("Standard");

        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.color = Color.white;
            trail.material = mat;
        }

        _isInitialized = true;
    }

    void Update()
    {
        if (!_isInitialized) return;

        _elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsedTime / _duration);

        // Update target position dynamically to track target movement during flight
        if (_target != null && _target.state != UnitState.Dead)
        {
            _targetPos = _target.transform.position + Vector3.up * _yOffset;
        }

        Vector3 currentPos = Vector3.Lerp(_startPos, _targetPos, t);

        // Parabolic arc height formula: y = lerp_y + 4 * H * t * (1 - t)
        float height = 4f * _arcHeight * t * (1f - t);
        currentPos.y += height;

        transform.position = currentPos;

        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[ArrowDebug] ArrowProjectile {name} Update: t={t}, pos={currentPos}");
        }

        // Calculate smooth analytical tangent to eliminate tracking jitter from target movement
        Vector3 tangent = (_targetPos - _startPos);
        tangent.y = (_targetPos.y - _startPos.y) + 4f * _arcHeight * (1f - 2f * t);

        if (tangent != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(tangent);
        }

        if (t >= 1.0f)
        {
            OnHit();
        }
    }

    private void OnHit()
    {
        Debug.Log($"[ArrowDebug] ArrowProjectile {name} OnHit target={(_target != null ? _target.name : "null")}");
        if (_target != null && _target.state != UnitState.Dead)
        {
            _target.TakeDamage(_damage);
        }
        
        // Spawn simple hit effect or particles if desired in future
        Destroy(gameObject);
    }
}
