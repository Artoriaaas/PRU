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

    [Header("Animation Tuning")]
    public float animSpeedMultiplier = 1f;

    public UnitState state = UnitState.Idle;

    private float _lastAttackTime;
    private Unit _target;
    private Rigidbody _rb;
    private Animator _animator;

    private Vector3 _rvoVelocity;
    private bool _useRVO = false;

    public Unit Target => _target;

    public void SetRVOVelocity(Vector3 velocity)
    {
        _rvoVelocity = velocity;
        _useRVO = true;
    }

    public Vector3 GetMoveTargetPosition()
    {
        if (_target == null) return transform.position;

        Vector3 targetPos = _target.transform.position;
        if (_myReservedSlot != null && _currentAttackTarget == _target)
        {
            targetPos = _currentAttackTarget.GetSlotWorldPosition(_myReservedSlot, this);
        }
        else
        {
            // Waiting stance outside the attack crowd: stand slightly outside the attack range
            Vector3 dirFromTarget = (transform.position - _target.transform.position).normalized;
            dirFromTarget.y = 0;
            if (dirFromTarget == Vector3.zero)
            {
                dirFromTarget = transform.forward;
            }
            float waitDistance = attackRange * 1.4f;
            targetPos = _target.transform.position + dirFromTarget * waitDistance;
        }
        return targetPos;
    }

    public class AttackSlot
    {
        public float angle; // Angle in degrees relative to forward
        public Unit reservedBy;
    }

    private List<AttackSlot> _attackSlots = new List<AttackSlot>();
    private Unit _currentAttackTarget;
    private AttackSlot _myReservedSlot;

    public void InitializeSlots()
    {
        _attackSlots.Clear();
        for (int i = 0; i < 6; i++)
        {
            _attackSlots.Add(new AttackSlot { angle = i * 60f, reservedBy = null });
        }
    }

    public Vector3 GetSlotWorldPosition(AttackSlot slot, Unit attacker = null)
    {
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        float radius = col != null ? col.radius * 2.2f : 12f;
        
        Unit activeAttacker = attacker != null ? attacker : slot.reservedBy;
        if (activeAttacker != null)
        {
            float maxAllowedRadius = activeAttacker.attackRange * 0.9f;
            if (radius > maxAllowedRadius)
            {
                radius = maxAllowedRadius;
            }
        }

        // Calculate offset based on slot angle in world space (relative to Vector3.forward)
        Vector3 direction = Quaternion.Euler(0, slot.angle, 0) * Vector3.forward;
        return transform.position + direction * radius;
    }

    public void ReserveSlotOnTarget(Unit target)
    {
        ReleaseReservedSlot();
        
        if (target == null) return;
        
        if (target._attackSlots.Count == 0)
        {
            target.InitializeSlots();
        }
        
        AttackSlot bestSlot = null;
        float minDistance = float.MaxValue;
        
        foreach (var slot in target._attackSlots)
        {
            if (slot.reservedBy == null)
            {
                Vector3 slotWorldPos = target.GetSlotWorldPosition(slot, this);
                float dist = Vector3.Distance(transform.position, slotWorldPos);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestSlot = slot;
                }
            }
        }
        
        if (bestSlot != null)
        {
            bestSlot.reservedBy = this;
            _myReservedSlot = bestSlot;
            _currentAttackTarget = target;
        }
    }

    public void ReleaseReservedSlot()
    {
        if (_myReservedSlot != null && _currentAttackTarget != null)
        {
            _myReservedSlot.reservedBy = null;
        }
        _myReservedSlot = null;
        _currentAttackTarget = null;
    }

    void SetTarget(Unit newTarget)
    {
        if (_target != newTarget)
        {
            ReleaseReservedSlot();
            _target = newTarget;
            if (_target != null)
            {
                ReserveSlotOnTarget(_target);
            }
        }
    }

    void OnDestroy()
    {
        ReleaseReservedSlot();
        foreach (var slot in _attackSlots)
        {
            if (slot.reservedBy != null)
            {
                slot.reservedBy.ReleaseReservedSlot();
            }
        }
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();
        InitializeSlots();
    }

    void Update()
    {
        if (state == UnitState.Dead) return;

        // Sync animator state variables
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", state == UnitState.Moving);
            _animator.SetBool("IsAttacking", state == UnitState.Attacking);

            if (state == UnitState.Moving)
            {
                float currentSpeed = 0f;
                if (_rb != null)
                {
                    currentSpeed = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z).magnitude;
                }
                else
                {
                    currentSpeed = speed;
                }

                float scaleFactor = 1.0f;
                CapsuleCollider col = GetComponent<CapsuleCollider>();
                if (col != null)
                {
                    scaleFactor = col.radius / 0.4f;
                }

                float naturalSpeed = 3.0f * scaleFactor;
                if (naturalSpeed < 0.01f) naturalSpeed = 0.01f;

                float speedRatio = (currentSpeed / naturalSpeed) * animSpeedMultiplier;
                _animator.speed = Mathf.Clamp(speedRatio, 0.15f, 3.0f);
            }
            else
            {
                _animator.speed = 1.0f;
            }
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

            if (_target == null || _target.state == UnitState.Dead || 
                (state != UnitState.Attacking && Time.frameCount % 60 == 0))
            {
                FindTarget();
            }

            if (_target != null)
            {
                // Dynamic slot reservation: periodically reclaim slot if we don't have one
                if (_myReservedSlot == null || _currentAttackTarget != _target)
                {
                    ReserveSlotOnTarget(_target);
                }
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
        float minDistanceWithFreeSlot = float.MaxValue;
        Unit bestTargetWithFreeSlot = null;

        float minDistanceAny = float.MaxValue;
        Unit bestTargetAny = null;

        foreach (var enemy in enemies)
        {
            if (enemy.state == UnitState.Dead) continue;
            
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDistanceAny)
            {
                minDistanceAny = dist;
                bestTargetAny = enemy;
            }

            if (enemy._attackSlots.Count == 0)
            {
                enemy.InitializeSlots();
            }

            bool hasFreeSlot = false;
            foreach (var slot in enemy._attackSlots)
            {
                if (slot.reservedBy == null || slot.reservedBy == this)
                {
                    hasFreeSlot = true;
                    break;
                }
            }

            if (hasFreeSlot && dist < minDistanceWithFreeSlot)
            {
                minDistanceWithFreeSlot = dist;
                bestTargetWithFreeSlot = enemy;
            }
        }

        Unit finalTarget = bestTargetWithFreeSlot != null ? bestTargetWithFreeSlot : bestTargetAny;
        SetTarget(finalTarget);
    }

    void MoveTowardsTarget()
    {
        if (_useRVO)
        {
            if (_rb != null)
            {
                Vector3 targetVelocity = new Vector3(_rvoVelocity.x, _rb.linearVelocity.y, _rvoVelocity.z);
                _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, Time.deltaTime * 15f);

                Vector3 direction = new Vector3(_rvoVelocity.x, 0, _rvoVelocity.z);
                if (direction.sqrMagnitude > 0.36f) // moving significantly
                {
                    Quaternion toRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 8f);
                }
                else if (_target != null) // almost stationary, face target to prevent spinning jitter
                {
                    Vector3 faceDir = (_target.transform.position - transform.position).normalized;
                    faceDir.y = 0;
                    if (faceDir != Vector3.zero)
                    {
                        Quaternion toRotation = Quaternion.LookRotation(faceDir, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 5f);
                    }
                }
            }
            else
            {
                transform.position += _rvoVelocity * Time.deltaTime;
            }

            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[RVO Diagnostic] {name} moving. RVO Velocity: {_rvoVelocity}");
            }
            return;
        }

        Vector3 targetPos = _target.transform.position;
        if (_myReservedSlot != null && _currentAttackTarget == _target)
        {
            targetPos = _currentAttackTarget.GetSlotWorldPosition(_myReservedSlot, this);
        }
        else
        {
            // Waiting stance outside the attack crowd: stand slightly outside the attack range
            Vector3 dirFromTarget = (transform.position - _target.transform.position).normalized;
            dirFromTarget.y = 0;
            if (dirFromTarget == Vector3.zero)
            {
                dirFromTarget = transform.forward;
            }
            float waitDistance = attackRange * 1.4f;
            targetPos = _target.transform.position + dirFromTarget * waitDistance;
        }
        
        Vector3 targetDir = (targetPos - transform.position).normalized;
        targetDir.y = 0;        // Keep movement on flat plane
        
        // Local Avoidance: steer away from nearby teammates to prevent overlapping
        Vector3 avoidance = Vector3.zero;
        Vector3 separation = Vector3.zero;
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
                    
                    // Lateral steering avoidance (perpendicular to targetDir)
                    Vector3 tangent = Vector3.Cross(Vector3.up, targetDir).normalized;
                    float dot = Vector3.Dot(diff, tangent);
                    
                    if (Mathf.Abs(dot) < 0.05f)
                    {
                        dot = Random.value > 0.5f ? 0.1f : -0.1f;
                    }
                    
                    Vector3 sideDir = tangent * Mathf.Sign(dot);
                    avoidance += sideDir * (avoidanceRange - dist);
                    
                    // Direct separation (flocking) force
                    if (dist > 0.01f)
                    {
                        separation += diff.normalized * ((avoidanceRange - dist) / avoidanceRange);
                    }
                    
                    neighborCount++;
                }
            }
        }
        
        Vector3 finalDirection = targetDir;
        if (neighborCount > 0 || separation != Vector3.zero)
        {
            // Combine target direction, lateral avoidance, and flocking separation
            finalDirection = (targetDir + avoidance * 1.5f + separation * 1.0f).normalized;
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
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, Time.deltaTime * 15f);
            
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
        if (state == UnitState.Attacking) return; // Attacking units are firmly planted and cannot be pushed
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Battle) return;
        if (_useRVO) return; // Skip manual push if using RVO collision avoidance
        
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
        
        SetTarget(null); // Release our reserved slot on target
        
        // Release all units targeting us
        foreach (var slot in _attackSlots)
        {
            if (slot.reservedBy != null)
            {
                slot.reservedBy.ReleaseReservedSlot();
            }
        }
        
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
