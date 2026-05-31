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

    void Update()
    {
        if (state == UnitState.Dead) return;

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
        transform.position += direction * speed * Time.deltaTime;
    }

    void Attack()
    {
        if (Time.time - _lastAttackTime >= attackCooldown)
        {
            _target.TakeDamage(atk);
            _lastAttackTime = Time.time;
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
        
        // Simple visual feedback: shrink and destroy
        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        float t = 0;
        Vector3 startScale = transform.localScale;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
        Destroy(gameObject);
    }
}
