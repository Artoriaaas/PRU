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
            if (_target == null || _target.state == UnitState.Dead)
            {
                FindTarget();
            }

            if (_target != null)
            {
                float distance = Vector3.Distance(transform.position, _target.transform.position);
                if (distance <= attackRange)
                {
                    state = UnitState.Attacking;
                    if (_rb != null) _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0); // Stop
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
                if (_rb != null) _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0); // Stop
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
        Vector3 direction = (_target.transform.position - transform.position).normalized;
        direction.y = 0; // Keep movement on flat plane
        
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[Diagnostic] {name} moving towards {_target.name}. direction: {direction}, speed: {speed}, rb: {(_rb != null)}, isKinematic: {(_rb != null && _rb.isKinematic)}, useGravity: {(_rb != null && _rb.useGravity)}, velocity: {(_rb != null ? _rb.linearVelocity : Vector3.zero)}");
        }

        if (_rb != null)
        {
            _rb.linearVelocity = new Vector3(direction.x * speed, _rb.linearVelocity.y, direction.z * speed);
            
            // Optionally, rotate towards target
            if (direction != Vector3.zero)
            {
                Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f);
            }
        }
        else
        {
            transform.position += direction * speed * Time.deltaTime;
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
        hp = 0;
        state = UnitState.Dead;
        GameManager.Instance.ReportDeath(this);
        
        if (_animator != null)
        {
            _animator.SetBool("IsDead", true);
            _animator.SetTrigger("Die");
        }

        // "Ragdoll" effect cho Capsule
        if (_rb != null)
        {
            _rb.isKinematic = false;
            if (_animator == null)
            {
                _rb.constraints = RigidbodyConstraints.None; // Bỏ khoá xoay
                Vector3 randomForce = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f)).normalized * 5f;
                _rb.AddForce(randomForce, ForceMode.Impulse);
                _rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
            }
            else
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;

                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }
        
        Destroy(gameObject, 2f);
    }
}
