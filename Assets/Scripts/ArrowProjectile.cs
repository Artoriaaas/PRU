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
    private bool _isInitialized = false;

    public void Initialize(Unit target, float damage, float speed, float arcHeight)
    {
        _target = target;
        _damage = damage;
        _speed = speed;
        _arcHeight = arcHeight;

        _startPos = transform.position;
        if (_target != null)
        {
            _targetPos = _target.transform.position + Vector3.up * 1.0f;
        }
        else
        {
            _targetPos = _startPos + transform.forward * 10f;
        }

        float distance = Vector3.Distance(_startPos, _targetPos);
        _duration = distance / _speed;
        if (_duration < 0.05f) _duration = 0.05f;
        _elapsedTime = 0f;

        // Configure TrailRenderer programmatically for the white streak effect
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
        }

        trail.time = 0.2f;
        trail.startWidth = 0.12f;
        trail.endWidth = 0.005f;
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
            _targetPos = _target.transform.position + Vector3.up * 1.0f;
        }

        Vector3 prevPos = transform.position;
        Vector3 currentPos = Vector3.Lerp(_startPos, _targetPos, t);

        // Parabolic arc height formula: y = lerp_y + 4 * H * t * (1 - t)
        float height = 4f * _arcHeight * t * (1f - t);
        currentPos.y += height;

        transform.position = currentPos;

        // Align arrow's rotation with its velocity vector
        Vector3 velocity = currentPos - prevPos;
        if (velocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }

        if (t >= 1.0f)
        {
            OnHit();
        }
    }

    private void OnHit()
    {
        if (_target != null && _target.state != UnitState.Dead)
        {
            _target.TakeDamage(_damage);
        }
        
        // Spawn simple hit effect or particles if desired in future
        Destroy(gameObject);
    }
}
