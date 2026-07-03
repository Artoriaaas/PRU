using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum UnitState { Idle, Moving, Attacking, Dead }

public class Unit : MonoBehaviour
{
    public bool isPlayer = true;
    public int unitTypeIndex = 0;
    [HideInInspector] public bool isSteeringAroundTeammate = false;
    public float hp = 100f;
    public float maxHp = 100f;
    public float atk = 10f;
    public float def = 5f;
    public float speed = 3f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Header("Animation Tuning")]
    public float animSpeedMultiplier = 1f;

    [Header("Archer Bow Settings")]
    public Vector3 bowRotOffsetIdle = new Vector3(344.91f, 87.90f, 338.19f);
    public Vector3 bowRotOffsetAttack = new Vector3(15.99f, 176.52f, 5.77f);
    
    private Transform _bowArmature;
    private Transform _leftHand;
    private Transform _graphicsTransform;
    private Quaternion _initialGraphicsRotation = Quaternion.identity;
    private Transform _rootBone; // For king run animation root motion XZ fix
    private float _defaultRootBoneLocalY; // Neutral Hips Y captured before animation plays
    private bool _rootBoneYCaptured; // Whether _defaultRootBoneLocalY has been set from the bind pose

    public UnitState state = UnitState.Idle;

    private float _lastAttackTime;
    private Unit _target;
    private Rigidbody _rb;
    private Animator _animator;
    private CapsuleCollider _myCollider;
    private float _baseColliderRadius = -1f;

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
        if (unitTypeIndex == 1) // Archer does not occupy slots and stands at 90% of attackRange
        {
            Vector3 dirFromTarget = (transform.position - _target.transform.position).normalized;
            dirFromTarget.y = 0;
            if (dirFromTarget == Vector3.zero)
            {
                dirFromTarget = transform.forward;
            }
            targetPos = _target.transform.position + dirFromTarget * (attackRange * 0.9f);
        }
        else if (_myReservedSlot != null && _currentAttackTarget == _target)
        {
            targetPos = _currentAttackTarget.GetSlotWorldPosition(_myReservedSlot, this);
        }
        else
        {
            // Waiting stance outside the attack crowd: stand further outside the attack range to prevent clumping
            Vector3 dirFromTarget = (transform.position - _target.transform.position).normalized;
            dirFromTarget.y = 0;
            if (dirFromTarget == Vector3.zero)
            {
                dirFromTarget = transform.forward;
            }
            
            // Add slight randomness to wait distance so they don't stack
            float randomOffset = Random.Range(0.2f, 1.0f);
            float waitDistance = attackRange * 2.5f + randomOffset;
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
        float radius = col != null ? col.radius * 2.5f : 15f;
        
        Unit activeAttacker = attacker != null ? attacker : slot.reservedBy;
        if (activeAttacker != null)
        {
            if (activeAttacker.unitTypeIndex == 1) // Archer
            {
                // Archer stands far away at 90% of their attack range
                radius = activeAttacker.attackRange * 0.9f;
            }
            else
            {
                float maxAllowedRadius = activeAttacker.attackRange * 1.0f;
                if (radius > maxAllowedRadius)
                {
                    radius = maxAllowedRadius;
                }
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

        // Archers do not occupy slots on their targets
        if (unitTypeIndex == 1)
        {
            _currentAttackTarget = target;
            return;
        }
        
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
        _myCollider = GetComponent<CapsuleCollider>();
        if (_myCollider != null)
        {
            _baseColliderRadius = _myCollider.radius;
        }
        InitializeSlots();

        // Cache the Hips bone immediately (before animation plays) so we can capture the
        // neutral localPosition.Y and prevent animation root curves from sinking the character.
        if (_animator != null)
        {
            foreach (Transform t in _animator.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLower().Contains("hips"))
                {
                    _rootBone = t;
                    _defaultRootBoneLocalY = t.localPosition.y;
                    _rootBoneYCaptured = true;
                    break;
                }
            }
        }

        // Cache the graphics model transform for archer alignment
        if (unitTypeIndex == 1)
        {
            foreach (Transform child in transform)
            {
                if (child.name.Contains("cung") || child.name.Contains("Model"))
                {
                    _graphicsTransform = child;
                    _initialGraphicsRotation = child.localRotation;
                    break;
                }
            }
        }
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
            else if (state == UnitState.Attacking)
            {
                // Speed up King's attack animation visually by 1.6x so it doesn't look too slow
                _animator.speed = (unitTypeIndex == 4) ? 1.6f : 1.0f;
            }
            else
            {
                _animator.speed = 1.0f;
            }
        }

        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Battle)
        {
            ClampToBoundaries();

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
                
                // Do not check teammate blocking once we are already attacking to prevent chattering from nearby teammates expanding their colliders.
                bool isBlocked = (state == UnitState.Attacking) ? false : IsPathBlockedByTeammate(_target.transform.position);
                
                if (distance <= currentRangeThreshold && !isBlocked)
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
                        // model_vua_after_update.fbx: model forward is -X at identity.
                        // LookRotation maps +Z→dir; offset Euler(0,90,0) maps -X→+Z so model faces dir.
                        if (unitTypeIndex == 4) toRotation *= Quaternion.Euler(0, 90, 0);
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

        // Archers bypass slot requirements completely and target the closest enemy
        if (unitTypeIndex == 1)
        {
            SetTarget(bestTargetAny);
            return;
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

                // Keep the model facing its target to prevent spinning jitter and side-facing behavior during avoidance
                if (_target != null)
                {
                    Vector3 faceDir = (_target.transform.position - transform.position).normalized;
                    faceDir.y = 0;
                    if (faceDir != Vector3.zero)
                    {
                        Quaternion toRotation = Quaternion.LookRotation(faceDir, Vector3.up);
                        if (unitTypeIndex == 4) toRotation *= Quaternion.Euler(0, 90, 0);
                        transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 8f);
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
        if (unitTypeIndex == 1) // Archer does not occupy slots and stands at 90% of attackRange
        {
            Vector3 dirFromTarget = (transform.position - _target.transform.position).normalized;
            dirFromTarget.y = 0;
            if (dirFromTarget == Vector3.zero)
            {
                dirFromTarget = transform.forward;
            }
            targetPos = _target.transform.position + dirFromTarget * (attackRange * 0.9f);
        }
        else if (_myReservedSlot != null && _currentAttackTarget == _target)
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
        float minX, maxX, minZ, maxZ;
        GetBoundaries(out minX, out maxX, out minZ, out maxZ);

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
            
            // Keep the model facing its target to prevent spinning jitter and side-facing behavior during avoidance
            if (_target != null)
            {
                Vector3 faceDir = (_target.transform.position - transform.position).normalized;
                faceDir.y = 0;
                if (faceDir != Vector3.zero)
                {
                    Quaternion toRotation = Quaternion.LookRotation(faceDir, Vector3.up);
                    if (unitTypeIndex == 4) toRotation *= Quaternion.Euler(0, 90, 0);
                    transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f);
                }
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
        
        // If we are using RVO, only allow manual trigger push if we are Attacking.
        // Moving units should rely on RVO to avoid active conflicts, but Attacking units (which have RVO prefVelocity = 0)
        // need manual trigger push to resolve overlaps with other attacking units.
        if (_useRVO && state != UnitState.Attacking) return;
        
        Unit otherUnit = other.GetComponentInParent<Unit>();
        // Only push teammates to prevent visual overlapping, do not push enemies during combat
        if (otherUnit != null && otherUnit.state != UnitState.Dead && otherUnit.isPlayer == this.isPlayer)
        {
            Vector3 pushDir = transform.position - otherUnit.transform.position;
            pushDir.y = 0;
            float dist = pushDir.magnitude;

            // Calculate the push threshold.
            // If both are attacking, we use their base radii (not expanded attack radii) 
            // to allow them to stand close to each other in adjacent slots without pushing.
            float myRadius = _myCollider != null ? _myCollider.radius : 0.4f;
            CapsuleCollider otherCol = otherUnit.GetComponent<CapsuleCollider>();
            float otherRadius = otherCol != null ? otherCol.radius : 0.4f;

            float minSafeDistance = myRadius + otherRadius;

            if (state == UnitState.Attacking)
            {
                float baseR1 = _baseColliderRadius > 0 ? _baseColliderRadius : 0.4f;
                float baseR2 = otherUnit._baseColliderRadius > 0 ? otherUnit._baseColliderRadius : 0.4f;

                // Scale up safe distance for archers to accommodate their wide bows and bodies
                if (unitTypeIndex == 1) baseR1 *= 2.2f;
                if (otherUnit.unitTypeIndex == 1) baseR2 *= 2.2f;

                minSafeDistance = (baseR1 + baseR2) * 1.05f;
            }

            if (dist >= minSafeDistance) return;

            // Skip push for very small overlaps (< 20% of safe distance) to prevent micro-jitter
            float overlap = minSafeDistance - dist;
            if (overlap < minSafeDistance * 0.2f) return;

            if (pushDir == Vector3.zero)
            {
                pushDir = new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
                dist = pushDir.magnitude;
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
            
            // Scale push amount based on overlap, but much gentler to prevent oscillation.
            // Use Rigidbody.position to avoid desyncing Unity's physics state.
            float scaleFactor = myRadius / 0.4f;
            float pushAmount = 0.015f * scaleFactor * (overlap / minSafeDistance);
            
            Vector3 newPos = transform.position + lateralPushDir * pushAmount;
            if (_rb != null)
            {
                _rb.position = newPos;
            }
            else
            {
                transform.position = newPos;
            }

            // Clamp immediately to prevent push from exceeding boundaries
            ClampToBoundaries();
        }
    }

    void LateUpdate()
    {
        if (state == UnitState.Dead) return;

        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Battle)
        {
            // Final clamp to prevent any physics/OnTriggerStay push from pushing units past the boundaries
            ClampToBoundaries();
        }

        // Fallback root bone find: if Awake didn't find it (e.g. graphics model parented after Awake),
        // retry on the first LateUpdate here and capture Y immediately.
        if (_rootBone == null && _animator != null)
        {
            foreach (var t in _animator.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLower().Contains("hips"))
                {
                    _rootBone = t;
                    if (!_rootBoneYCaptured)
                    {
                        _animator.Play("Idle", 0, 0f);
                        _animator.Update(0f);
                        _defaultRootBoneLocalY = t.localPosition.y;
                        _rootBoneYCaptured = true;
                    }
                    break;
                }
            }
        }

        // Root bone position drift fix: reset Hips to neutral Y position to prevent animation
        // root curves from sinking the character. Only reset Y during Idle to avoid fighting
        // with attack/death animations that have intentional Y movement.
        // Reset XZ during Moving for king only to prevent sliding.
        if (_rootBone != null)
        {
            Vector3 pos = _rootBone.localPosition;
            if (state == UnitState.Moving && unitTypeIndex == 4)
            {
                pos.x = 0;
                pos.z = 0;
            }
            if (state == UnitState.Idle)
            {
                pos.y = _defaultRootBoneLocalY;
            }
            _rootBone.localPosition = pos;
        }


        // Align archer bow dynamically based on combat state
        if (unitTypeIndex == 1)
        {
            UpdateGraphicsRotation();
            UpdateBowAlignment();
        }
    }

    private void UpdateGraphicsRotation()
    {
        if (_graphicsTransform == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name.Contains("cung") || child.name.Contains("Model"))
                {
                    _graphicsTransform = child;
                    _initialGraphicsRotation = child.localRotation;
                    break;
                }
            }
        }

        if (_graphicsTransform == null) return;
        
        Quaternion targetLocalRot;
        if (state == UnitState.Attacking)
        {
            // Rotate by 90 degrees on Y axis to align the animation's sideways shooting with the target forward direction
            targetLocalRot = _initialGraphicsRotation * Quaternion.Euler(0f, 90f, 0f);
        }
        else
        {
            targetLocalRot = _initialGraphicsRotation;
        }
        
        _graphicsTransform.localRotation = Quaternion.Slerp(_graphicsTransform.localRotation, targetLocalRot, Time.deltaTime * 10f);
    }

    private void UpdateBowAlignment()
    {
        // Bypassed: The new archer model has the bow integrated as a SkinnedMeshRenderer on the skeleton.
        // It is animated natively by the clips (Generic rig), so manual rotation/positioning is not needed.
        return;
        
        Debug.Log($"[BowDebug] UpdateBowAlignment running on {name}. state={state}, target={(_target != null ? _target.name : "null")}, offsetIdle={bowRotOffsetIdle}");
        if (_leftHand == null)
        {
            Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in childTransforms)
            {
                if (t.name == "mixamorig:LeftHand")
                {
                    _leftHand = t;
                    break;
                }
            }
        }
        
        if (_leftHand != null && _bowArmature == null)
        {
            _bowArmature = _leftHand.Find("Armature");
        }
        
        if (_bowArmature != null)
        {
            if (state == UnitState.Attacking && _target != null)
            {
                Vector3 targetDirection = (_target.transform.position - transform.position).normalized;
                targetDirection.y = 0;
                if (targetDirection == Vector3.zero) targetDirection = transform.forward;

                // Align bow's upward axis (local +Z) with Vector3.up
                // and bow's shooting axis (local -X) with targetDirection (so local +X points to -targetDirection)
                Vector3 localY = Vector3.Cross(targetDirection, Vector3.up).normalized;
                _bowArmature.rotation = Quaternion.LookRotation(Vector3.up, localY);
            }
            else
            {
                Vector3 eulerAngles = bowRotOffsetIdle;
                _bowArmature.localRotation = Quaternion.Euler(eulerAngles);
            }
            
            Transform bowBone = _bowArmature.Find("Bone");
            Vector3 boneLocalPos = bowBone != null ? bowBone.localPosition : Vector3.zero;
            _bowArmature.localPosition = -(_bowArmature.localRotation * boneLocalPos);
            _bowArmature.localScale = Vector3.one;
        }
    }

    void Attack()
    {
        if (Time.time - _lastAttackTime >= attackCooldown)
        {
            _lastAttackTime = Time.time;
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }

            if (unitTypeIndex == 1) // Archer
            {
                float animSpeed = _animator != null ? _animator.speed : 1f;
                float delay = 0.4f / Mathf.Max(0.1f, animSpeed);
                StartCoroutine(SpawnArrowAfterDelay(delay, _target, atk));
            }
            else // Melee
            {
                if (_target != null && _target.state != UnitState.Dead)
                {
                    _target.TakeDamage(atk);
                }
            }
        }
    }

    private IEnumerator SpawnArrowAfterDelay(float delay, Unit target, float damage)
    {
        Debug.Log($"[ArrowDebug] SpawnArrowAfterDelay started on {name}. delay={delay}, target={(target != null ? target.name : "null")}");
        yield return new WaitForSeconds(delay);

        if (target == null && _target != null)
        {
            target = _target;
        }

        Debug.Log($"[ArrowDebug] SpawnArrowAfterDelay yield finished on {name}. target={(target != null ? target.name : "null")}");

        if (_leftHand == null)
        {
            Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in childTransforms)
            {
                if (t.name == "mixamorig:LeftHand")
                {
                    _leftHand = t;
                    break;
                }
            }
        }

        Vector3 spawnPos = transform.position + Vector3.up * 1.5f;
        if (_leftHand != null)
        {
            spawnPos = _leftHand.position;
        }

        GameObject arrowPrefab = null;
        float speed = 25f;
        float arcHeight = 2.5f;
        float scale = 60f;

        if (GameManager.Instance != null)
        {
            arrowPrefab = GameManager.Instance.arrowPrefab;
            speed = GameManager.Instance.arrowSpeed;
            arcHeight = GameManager.Instance.arrowArcHeight;
            scale = GameManager.Instance.archerScale;
        }

        // Adjust spawnPos offset height based on scale
        spawnPos.y += 0.5f * (scale / 60f);

        Debug.Log($"[ArrowDebug] Spawning arrow. prefab={(arrowPrefab != null ? arrowPrefab.name : "null")}, speed={speed}, arcHeight={arcHeight}, scale={scale}, pos={spawnPos}");

        GameObject arrowObj = new GameObject("ArrowContainer");
        arrowObj.transform.position = spawnPos;

        if (arrowPrefab != null)
        {
            GameObject visual = Instantiate(arrowPrefab, arrowObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // Point X-axis arrow mesh (tip at -X) forward along container Z-axis
            visual.transform.localScale = arrowPrefab.transform.localScale * (scale * 1.5f); // Scale arrow visuals to match 60x enlarged world
            
            // Destroy Animator on the visual to prevent it from overriding localRotation back to prefab default (-90 on X)
            Animator anim = visual.GetComponent<Animator>();
            if (anim != null) DestroyImmediate(anim);
            
            // Fix URP shader compatibility to prevent bright magenta rendering
            Renderer[] rends = visual.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (r.material != null)
                {
                    Shader urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
                    if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpShader == null) urpShader = Shader.Find("Standard");
                    
                    if (urpShader != null)
                    {
                        Texture mainTex = r.material.mainTexture;
                        r.material.shader = urpShader;
                        if (mainTex != null)
                        {
                            r.material.SetTexture("_BaseMap", mainTex);
                            r.material.SetTexture("_MainTex", mainTex);
                        }
                    }
                }
            }
        }
        else
        {
            arrowObj.transform.localScale = new Vector3(scale, scale, scale);

            // Long, thin cylinder as the arrow body
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.transform.SetParent(arrowObj.transform, false);
            visual.transform.localScale = new Vector3(0.04f, 0.4f, 0.04f); // relative to container
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Point Y-up cylinder forward along Z-axis

            Collider c = visual.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Sprites/Default"));
                rend.material.color = Color.white;
            }
        }

        ArrowProjectile arrow = arrowObj.GetComponent<ArrowProjectile>();
        if (arrow == null)
        {
            arrow = arrowObj.AddComponent<ArrowProjectile>();
        }

        arrow.Initialize(target, damage, speed, arcHeight, scale);
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
        
        float destroyDelay = 2f;
        if (_animator != null)
        {
            _animator.SetBool("IsDead", true);
            _animator.SetBool("IsMoving", false);
            _animator.SetBool("IsAttacking", false);
            _animator.SetTrigger("Die");
            Debug.Log($"[PRU Debug] {name} animator parameters set: IsDead=true, Die trigger fired.");
            
            // Set destroy delay to the exact length of the dying animation clip
            var clips = _animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip.name.ToLower().Contains("die") || clip.name.ToLower().Contains("dying") || clip.name.ToLower().Contains("death"))
                {
                    destroyDelay = Mathf.Max(destroyDelay, clip.length);
                }
            }
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
        
        Debug.Log($"[PRU Debug] {name} destroying in {destroyDelay}s");
        Destroy(gameObject, destroyDelay);
    }

    private bool IsPathBlockedByTeammate(Vector3 targetPos)
    {
        // Ranged units (Archers) can attack over teammates, so they are never blocked
        if (unitTypeIndex == 1) return false;
        
        if (GameManager.Instance == null) return false;

        CapsuleCollider col = GetComponent<CapsuleCollider>();
        float myRadius = col != null ? Mathf.Min(col.radius, 2.0f) : 0.4f;

        Vector3 A = transform.position;
        Vector3 B = targetPos;
        A.y = 0;
        B.y = 0;

        Vector3 AB = B - A;
        float abDistance = AB.magnitude;
        if (abDistance < 0.01f) return false;
        Vector3 dir = AB / abDistance;

        // The check segment starts from the front face of the unit, not its center
        Vector3 A_prime = A + dir * myRadius;
        Vector3 A_prime_B = B - A_prime;
        float aPrimeBDistance = A_prime_B.magnitude;
        if (aPrimeBDistance < 0.01f) return false;
        Vector3 dirPrime = A_prime_B / aPrimeBDistance;

        List<Unit> teammates = isPlayer ? GameManager.Instance.playerUnits : GameManager.Instance.enemyUnits;
        foreach (var teammate in teammates)
        {
            if (teammate == this || teammate.state == UnitState.Dead) continue;

            Vector3 C = teammate.transform.position;
            C.y = 0;

            Vector3 A_prime_C = C - A_prime;
            float proj = Vector3.Dot(A_prime_C, dirPrime);

            // Only consider teammates that are in front of our front face and not behind the target
            if (proj > 0.05f && proj < aPrimeBDistance - 0.05f)
            {
                Vector3 P = A_prime + proj * dirPrime;
                float distToSegment = Vector3.Distance(C, P);

                CapsuleCollider teamCol = teammate.GetComponent<CapsuleCollider>();
                float teamRadius = teamCol != null ? Mathf.Min(teamCol.radius, 2.0f) : 0.4f;

                // We need to account for both our radius and the teammate's radius.
                // If the distance from the teammate's center to the path segment is less than the sum of their radii,
                // the unit's body would overlap/collide with the teammate when moving along this path.
                float combinedRadius = myRadius + teamRadius;

                // Apply hysteresis: use a tighter blocking radius if we are already attacking to prevent oscillation.
                // Using 0.9f for Moving and 0.6f for Attacking guarantees a stable positive hysteresis buffer in all states,
                // preventing chattering/oscillation at the boundaries.
                float blockFactor = (state == UnitState.Attacking) ? 0.6f : 0.9f;

                if (distToSegment < combinedRadius * blockFactor)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void GetBoundaries(out float minX, out float maxX, out float minZ, out float maxZ)
    {
        minX = -5.2f;
        maxX = 5.2f;
        minZ = -17f;
        maxZ = 25f;

        BattlefieldGridGenerator gridGen = Object.FindAnyObjectByType<BattlefieldGridGenerator>();
        if (gridGen != null)
        {
            minX = -1100f;
            maxX = 1100f;
            maxZ = 230f;

            float colRadius = 0.4f;
            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                colRadius = col.radius;
            }

            // Staggered background wall boundaries (Quad_Enemy, Quad, Quad_Player)
            // Add safety offset of +1.0f + colRadius so the visual mesh never penetrates the quad.
            float x = transform.position.x;
            if (x < -310f)
            {
                minZ = -36.25f + colRadius + 1.0f;
            }
            else if (x <= 250f)
            {
                minZ = -42.00f + colRadius + 1.0f;
            }
            else
            {
                minZ = -45.99f + colRadius + 1.0f;
            }
        }
    }

    private void ClampToBoundaries()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Battle) return;
        float minX, maxX, minZ, maxZ;
        GetBoundaries(out minX, out maxX, out minZ, out maxZ);
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        float clampedZ = Mathf.Clamp(transform.position.z, minZ, maxZ);
        transform.position = new Vector3(clampedX, transform.position.y, clampedZ);
    }
}
