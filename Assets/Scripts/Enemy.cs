using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Base enemy that walks a path and attacks.</summary>
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Identity")]
    public EnemyType type = EnemyType.Grunt;
    public int reward = 15;

    [Header("Health")]
    public float maxHealth = 30f;
    public float healthBarHeight = 1.3f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float waypointTolerance = 0.3f;
    [Range(0f, 1f)] public float slowResistance = 0f;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float damage = 8f;
    public float attacksPerSecond = 1f;
    public GameObject projectilePrefab;

    [Header("Positioning")]
    public float heightOffset = 0.5f;

    public static event Action<Enemy> OnEnemyKilled;

    private static readonly Color SlowColor = new Color(0.45f, 0.9f, 1f);

    private float currentHealth;
    private float attackCooldown;
    private List<Vector3> path;
    private int waypointIndex;
    private float slowFactor = 1f;
    private float slowTimer;
    private LineRenderer slowRing;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;
    public Vector3 Velocity { get; private set; }

    // 0 = just spawned, 1 = reached the tower. Used to measure how deep enemies get.
    public float PathProgress => path == null || path.Count == 0 ? 0f : (float)waypointIndex / path.Count;

    protected float CurrentSpeed => moveSpeed * slowFactor;

    protected virtual void Awake()
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

    // Called by the wave spawner so later waves have tougher enemies.
    public void ScaleHealth(float multiplier)
    {
        maxHealth *= multiplier;
        currentHealth = maxHealth;
    }

    protected virtual void Update()
    {
        if (IsDead || path == null) return;

        Velocity = Vector3.zero;
        UpdateSlow();
        attackCooldown -= Time.deltaTime;

        Collider target = FindTarget();
        if (target != null && attackCooldown <= 0f)
        {
            Attack(target);
            attackCooldown = 1f / attacksPerSecond;
        }

        if (target == null || !StopsToAttack(target))
            MoveAlongPath();
    }

    protected virtual Collider FindTarget()
    {
        return Targeting.FindClosest(transform.position, attackRange, "Tower", "Defender");
    }

    protected virtual bool StopsToAttack(Collider target) => true;

    protected virtual void Attack(Collider target)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();

        if (projectilePrefab == null)
        {
            damageable?.TakeDamage(damage);
            return;
        }

        GameObject projectileObject = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectileObject.GetComponent<Projectile>()?.Launch(target.transform, damageable, damage);
    }

    private void MoveAlongPath()
    {
        if (waypointIndex >= path.Count) return;

        Vector3 targetPoint = path[waypointIndex] + Vector3.up * heightOffset;
        Vector3 previous = transform.position;
        transform.position = Vector3.MoveTowards(previous, targetPoint, CurrentSpeed * Time.deltaTime);

        Vector3 step = transform.position - previous;
        if (Time.deltaTime > 0f)
            Velocity = step / Time.deltaTime;

        step.y = 0f;
        if (step.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(step);

        if (Vector3.Distance(transform.position, targetPoint) < waypointTolerance)
            waypointIndex++;
    }

    public void ApplySlow(float strength, float duration)
    {
        float resisted = strength * (1f - slowResistance);
        slowFactor = Mathf.Min(slowFactor, 1f - resisted);
        slowTimer = Mathf.Max(slowTimer, duration);

        if (slowRing == null)
        {
            GameObject ringObject = new GameObject("SlowRing");
            ringObject.transform.SetParent(transform, false);
            slowRing = RingVisual.Create(ringObject.transform, SlowColor, 0.08f);
            RingVisual.SetCircle(slowRing, Vector3.down * (heightOffset - 0.1f), 0.6f);
        }

        slowRing.enabled = true;
    }

    private void UpdateSlow()
    {
        if (slowTimer <= 0f) return;

        slowTimer -= Time.deltaTime;
        if (slowTimer <= 0f)
        {
            slowFactor = 1f;
            slowRing.enabled = false;
        }
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
        bar.offset = new Vector3(0f, healthBarHeight, 0f);
        bar.borderColor = new Color(0.55f, 0.05f, 0.05f);
        bar.Initialize(this);
    }
}
