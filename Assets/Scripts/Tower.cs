using System;
using UnityEngine;

/// <summary>Central tower that shoots enemies.</summary>
public class Tower : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Attack")]
    public float attackRange = 8f;
    public float damage = 10f;
    public float attacksPerSecond = 1f;
    public GameObject projectilePrefab;

    [Header("Positioning")]
    public float heightOffset = 1f;
    public float healthBarHeight = 1.6f;

    public static event Action OnTowerDestroyed;
    public static event Action<float, float> OnTowerHealthChanged;

    private float currentHealth;
    private float attackCooldown;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
        transform.position += Vector3.up * heightOffset;
        SpawnHealthBar();
    }

    private void Start()
    {
        // Lets the HUD and the player tracker know the new tower's health straight away.
        OnTowerHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (IsDead) return;

        attackCooldown -= Time.deltaTime;
        if (attackCooldown > 0f) return;

        Collider target = FindClosestEnemy();
        if (target == null) return;

        FireAt(target);
        attackCooldown = 1f / attacksPerSecond;
    }

    private Collider FindClosestEnemy()
    {
        return Targeting.FindClosest(transform.position, attackRange, "Enemy");
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
        OnTowerHealthChanged?.Invoke(currentHealth, maxHealth);
        GetComponent<HitFlash>()?.Flash();

        if (IsDead)
            OnTowerDestroyed?.Invoke();
    }

    private void SpawnHealthBar()
    {
        GameObject barObject = new GameObject("HealthBar");
        barObject.transform.SetParent(transform);
        WorldHealthBar bar = barObject.AddComponent<WorldHealthBar>();
        bar.offset = new Vector3(0f, healthBarHeight, 0f);
        bar.size = new Vector2(2f, 0.3f);
        bar.borderColor = new Color(0.95f, 0.75f, 0.2f);
        bar.Initialize(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}