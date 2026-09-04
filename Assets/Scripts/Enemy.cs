// Enemy.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 30f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float waypointTolerance = 0.3f;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float damage = 8f;
    public float attacksPerSecond = 1f;
    public GameObject projectilePrefab;

    [Header("Positioning")]
    public float heightOffset = 0.5f;

    public static event Action<Enemy> OnEnemyKilled;

    private float currentHealth;
    private float attackCooldown;
    private List<Vector3> path;
    private int waypointIndex;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
        transform.position += Vector3.up * heightOffset;
        SpawnHealthBar();
    }

    public void SetPath(List<Vector3> assignedPath)
    {
        path = assignedPath;
        waypointIndex = 0;
    }

    private void Update()
    {
        if (IsDead || path == null) return;

        attackCooldown -= Time.deltaTime;

        Collider target = FindTargetInRange();
        if (target != null)
        {
            if (attackCooldown <= 0f)
            {
                FireAt(target);
                attackCooldown = 1f / attacksPerSecond;
            }
            return;
        }

        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (waypointIndex >= path.Count) return;

        Vector3 targetPoint = path[waypointIndex] + Vector3.up * heightOffset;
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPoint) < waypointTolerance)
            waypointIndex++;
    }

    private Collider FindTargetInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        Collider closest = null;
        float closestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Tower") && !hit.CompareTag("Defender")) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsDead) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit;
            }
        }

        return closest;
    }

    private void FireAt(Collider targetCollider)
    {
        IDamageable damageable = targetCollider.GetComponent<IDamageable>();

        if (projectilePrefab == null)
        {
            damageable?.TakeDamage(damage);
            return;
        }

        GameObject projectileObject = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectileObject.GetComponent<Projectile>()?.Launch(targetCollider.transform, damageable, damage);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        GetComponent<HitFlash>()?.Flash();

        if (IsDead)
        {
            OnEnemyKilled?.Invoke(this);
            Destroy(gameObject);
        }
    }

    private void SpawnHealthBar()
    {
        GameObject barObject = new GameObject("HealthBar");
        barObject.transform.SetParent(transform);
        WorldHealthBar bar = barObject.AddComponent<WorldHealthBar>();
        bar.Initialize(this);
    }
}