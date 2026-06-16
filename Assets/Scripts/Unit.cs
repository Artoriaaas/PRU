using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum UnitState { Idle, Moving, Attacking, Dead }

public class Unit : MonoBehaviour
{
    public bool isPlayer = true;
    public float hp = 100f;
    public float maxHp = 100f;
    public float atk = 10f;
    public float def = 5f;
    public float speed = 3f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    public UnitState state = UnitState.Idle;

    private float _lastAttackTime;
    private Unit _target;
    private Rigidbody _rb;
    private Animator _animator;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (state == UnitState.Dead) return;

        // Sync animator state variables
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", state == UnitState.Moving);
            _animator.SetBool("IsAttacking", state == UnitState.Attacking);
        }

        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Battle)
        {
            // Dynamic boundaries based on the scene layout scale (large grid system vs legacy bootstrapper)
            float minX = -5.2f;
            float maxX = 5.2f;
            float minZ = -17f;
            float maxZ = 25f;

            BattlefieldGridGenerator gridGen = Object.FindAnyObjectByType<BattlefieldGridGenerator>();
            if (gridGen != null)
            {
                minX = -1100f;
                maxX = 1100f;
                minZ = -145f;
                maxZ = 230f;
            }

            // Clamp position within arena boundaries to prevent walking through background quads or walls
            float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
            float clampedZ = Mathf.Clamp(transform.position.z, minZ, maxZ);
            transform.position = new Vector3(clampedX, transform.position.y, clampedZ);

            if (_target == null || _target.state == UnitState.Dead)
            {
                FindTarget();
            }

            if (_target != null)
            {
                float distance = Vector3.Distance(transform.position, _target.transform.position);
                
                // Hysteresis threshold to prevent chattering/oscillation pushing behavior
                float currentRangeThreshold = (state == UnitState.Attacking) ? (attackRange * 1.25f) : attackRange;
                
                if (distance <= currentRangeThreshold)
                {
                    state = UnitState.Attacking;
                    if (_rb != null)
                    {
                        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0); // Stop horizontal movement but allow gravity
                    }
                    
                    // Rotate towards target during attack
                    Vector3 direction = (_target.transform.position - transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f);
                    }

                    Attack();
                }
                else
                {
                    state = UnitState.Moving;
                    MoveTowardsTarget();
                }
            }
            else
            {
                state = UnitState.Idle;
                if (_rb != null)
                {
                    _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0); // Stop horizontal movement but allow gravity
                }
            }
        }
    }

    void FindTarget()
    {
        List<Unit> enemies = isPlayer ? GameManager.Instance.enemyUnits : GameManager.Instance.playerUnits;
        float minDistance = float.MaxValue;
        Unit bestTarget = null;

        foreach (var enemy in enemies)
        {
            if (enemy.state == UnitState.Dead) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestTarget = enemy;
            }
        }

        _target = bestTarget;
    }

    void MoveTowardsTarget()
    {
        Vector3 targetDir = (_target.transform.position - transform.position).normalized;
        targetDir.y = 0; // Keep movement on flat plane
        
        // Local Avoidance: steer away from nearby teammates to prevent overlapping
        Vector3 avoidance = Vector3.zero;
        int neighborCount = 0;
        
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        float avoidanceRange = col != null ? col.radius * 2.2f : 1.2f;
        
        if (GameManager.Instance != null)
        {
            List<Unit> teammates = isPlayer ? GameManager.Instance.playerUnits : GameManager.Instance.enemyUnits;
            foreach (var teammate in teammates)
            {
                if (teammate == this || teammate.state == UnitState.Dead) continue;
                
                float dist = Vector3.Distance(transform.position, teammate.transform.position);
                if (dist < avoidanceRange)
                {
                    Vector3 diff = transform.position - teammate.transform.position;
                    diff.y = 0;
                    
                    // Project avoidance vector laterally (perpendicular to targetDir) to only push sideways
                    Vector3 tangent = Vector3.Cross(Vector3.up, targetDir).normalized;
                    float dot = Vector3.Dot(diff, tangent);
                    
                    // If they are exactly aligned, choose a random side
                    if (Mathf.Abs(dot) < 0.05f)
                    {
                        dot = Random.value > 0.5f ? 0.1f : -0.1f;
                    }
                    
                    Vector3 sideDir = tangent * Mathf.Sign(dot);
                    avoidance += sideDir * (avoidanceRange - dist);
                    neighborCount++;
                }
            }
        }
        
        Vector3 finalDirection = targetDir;
        if (neighborCount > 0)
        {
            // Combine target direction and lateral avoidance.
            // Weighting avoidance makes them naturally slide around each other laterally.
            finalDirection = (targetDir + avoidance * 2.0f).normalized;
        }

        // Slide along boundaries if trying to move past them to prevent getting stuck
        float minX = -5.2f;
        float maxX = 5.2f;
        float minZ = -17f;
        float maxZ = 25f;

        BattlefieldGridGenerator gridGen = Object.FindAnyObjectByType<BattlefieldGridGenerator>();
        if (gridGen != null)
        {
            minX = -1100f;
            maxX = 1100f;
            minZ = -145f;
            maxZ = 230f;
        }

        if (transform.position.z <= minZ && finalDirection.z < 0)
        {
            finalDirection.z = 0;
            if (finalDirection != Vector3.zero) finalDirection = finalDirection.normalized;
        }
        else if (transform.position.z >= maxZ && finalDirection.z > 0)
        {
            finalDirection.z = 0;
            if (finalDirection != Vector3.zero) finalDirection = finalDirection.normalized;
        }

        if (transform.position.x <= minX && finalDirection.x < 0)
        {
            finalDirection.x = 0;
            if (finalDirection != Vector3.zero) finalDirection = finalDirection.normalized;
        }
        else if (transform.position.x >= maxX && finalDirection.x > 0)
        {
            finalDirection.x = 0;
            if (finalDirection != Vector3.zero) finalDirection = finalDirection.normalized;
        }
        
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[Diagnostic] {name} moving towards {_target.name}. finalDirection: {finalDirection}, speed: {speed}");
        }

        if (_rb != null)
        {
            Vector3 targetVelocity = new Vector3(finalDirection.x * speed, _rb.linearVelocity.y, finalDirection.z * speed);
            // Smoothly interpolate velocity to eliminate high-frequency jitter/stutters
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, Time.deltaTime * 6f);
            
            // Optionally, rotate towards target
            if (finalDirection != Vector3.zero)
            {
                Quaternion toRotation = Quaternion.LookRotation(finalDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f);
            }
        }
        else
        {
            transform.position += finalDirection * speed * Time.deltaTime;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (state == UnitState.Dead) return;
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Battle) return;
        
        Unit otherUnit = other.GetComponentInParent<Unit>();
        // Only push teammates to prevent visual overlapping, do not push enemies during combat
        if (otherUnit != null && otherUnit.state != UnitState.Dead && otherUnit.isPlayer == this.isPlayer)
        {
            Vector3 pushDir = transform.position - otherUnit.transform.position;
            pushDir.y = 0;
            if (pushDir == Vector3.zero)
            {
                pushDir = new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
            }
            
            // Project push direction laterally (perpendicular to targetDir) to prevent pushing units backwards
            Vector3 targetDir = _target != null ? (_target.transform.position - transform.position).normalized : transform.forward;
            targetDir.y = 0;
            Vector3 tangent = Vector3.Cross(Vector3.up, targetDir).normalized;
            float dot = Vector3.Dot(pushDir, tangent);
            
            // If they are exactly aligned, choose a random side
            if (Mathf.Abs(dot) < 0.05f)
            {
                dot = Random.value > 0.5f ? 0.1f : -0.1f;
            }
            Vector3 lateralPushDir = tangent * Mathf.Sign(dot);
            
            // Gently push them apart by modifying position.
            // Since they are triggers, this is extremely smooth and won't conflict with physics solver!
            CapsuleCollider col = GetComponent<CapsuleCollider>();
            float scaleFactor = col != null ? col.radius / 0.4f : 1.0f;
            float pushAmount = 0.04f * scaleFactor * Time.deltaTime * 60f; // framerate independent
            if (state == UnitState.Attacking)
            {
                pushAmount *= 0.5f;
            }
            transform.position += lateralPushDir * pushAmount;

            // Clamp immediately to prevent push from exceeding boundaries
            float minX = -5.2f;
            float maxX = 5.2f;
            float minZ = -17f;
            float maxZ = 25f;

            BattlefieldGridGenerator gridGen = Object.FindAnyObjectByType<BattlefieldGridGenerator>();
            if (gridGen != null)
            {
                minX = -1100f;
                maxX = 1100f;
                minZ = -145f;
                maxZ = 230f;
            }
            float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
            float clampedZ = Mathf.Clamp(transform.position.z, minZ, maxZ);
            transform.position = new Vector3(clampedX, transform.position.y, clampedZ);
        }
    }

    void LateUpdate()
    {
        if (state == UnitState.Dead) return;

        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Battle)
        {
            // Dynamic boundaries based on the scene layout scale (large grid system vs legacy bootstrapper)
            float minX = -5.2f;
            float maxX = 5.2f;
            float minZ = -17f;
            float maxZ = 25f;

            BattlefieldGridGenerator gridGen = Object.FindAnyObjectByType<BattlefieldGridGenerator>();
            if (gridGen != null)
            {
                minX = -1100f;
                maxX = 1100f;
                minZ = -145f;
                maxZ = 230f;
            }

            // Final clamp to prevent any physics/OnTriggerStay push from pushing units past the boundaries
            float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
            float clampedZ = Mathf.Clamp(transform.position.z, minZ, maxZ);
            transform.position = new Vector3(clampedX, transform.position.y, clampedZ);
        }
    }

    void Attack()
    {
        if (Time.time - _lastAttackTime >= attackCooldown)
        {
            _target.TakeDamage(atk);
            _lastAttackTime = Time.time;
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (state == UnitState.Dead) return;

        float actualDamage = Mathf.Max(1f, damage - def);
        hp -= actualDamage;

        if (hp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"[PRU Debug] {name} has entered Die()! state={state}, hp={hp}, _animator={(_animator != null ? _animator.name : "null")}, _rb={_rb != null}");
        
        hp = 0;
        state = UnitState.Dead;
        GameManager.Instance.ReportDeath(this);
        
        if (_animator != null)
        {
            _animator.SetBool("IsDead", true);
            _animator.SetBool("IsMoving", false);
            _animator.SetBool("IsAttacking", false);
            _animator.SetTrigger("Die");
            Debug.Log($"[PRU Debug] {name} animator parameters set: IsDead=true, Die trigger fired.");
        }
        else
        {
            Debug.LogWarning($"[PRU Debug] {name} has NO animator inside Die()!");
        }

        // "Ragdoll" effect cho Capsule
        if (_rb != null)
        {
            if (_animator == null)
            {
                _rb.isKinematic = false;
                _rb.constraints = RigidbodyConstraints.None; // Bỏ khoá xoay
                Vector3 randomForce = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f)).normalized * 5f;
                _rb.AddForce(randomForce, ForceMode.Impulse);
                _rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
                Debug.Log($"[PRU Debug] {name} ragdoll force applied.");
            }
            else
            {
                _rb.isKinematic = true;
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;

                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                Debug.Log($"[PRU Debug] {name} kinematic set to true and collider disabled.");
            }
        }
        
        Debug.Log($"[PRU Debug] {name} destroying in 2.0s");
        Destroy(gameObject, 2f);
    }
}
