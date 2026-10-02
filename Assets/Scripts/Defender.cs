using System;
using UnityEngine;

/// <summary>Base defender that shoots enemies in range.</summary>
public class Defender : MonoBehaviour, IDamageable
{
    [Header("Shop")]
    public DefenderType type = DefenderType.Gunner;
    public string displayName = "Gunner";
    public int cost = 20;
    public string description = "Fast single-target shots";
    public Sprite icon;
    public Color accentColor = new Color(0.2f, 0.5f, 1f);

    [Header("Health")]
    public float maxHealth = 40f;
    public float healthBarHeight = 1.3f;

    [Header("Attack")]
    public float attackRange = 6f;
    public float damage = 12f;
    public float attacksPerSecond = 1.5f;
    public GameObject projectilePrefab;

    [Header("Positioning")]
    public float heightOffset = 1f;

    public static event Action<Defender> OnDefenderDestroyed;

    private float currentHealth;
    private float attackCooldown;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        transform.position += Vector3.up * heightOffset;
        SpawnHealthBar();
    }

    protected virtual void Update()
    {
        if (IsDead) return;

        attackCooldown -= Time.deltaTime;
        if (attackCooldown > 0f) return;

        Collider target = FindTarget();
        if (target == null) return;

        Attack(target);
        attackCooldown = 1f / attacksPerSecond;
    }

    protected virtual Collider FindTarget()
    {
        return Targeting.FindClosest(transform.position, attackRange, "Enemy");
    }

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

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        GetComponent<HitFlash>()?.Flash();

        if (IsDead)
        {
            OnDefenderDestroyed?.Invoke(this);
            Destroy(gameObject);
        }
    }

    private void SpawnHealthBar()
    {
        GameObject barObject = new GameObject("HealthBar");
        barObject.transform.SetParent(transform);
        WorldHealthBar bar = barObject.AddComponent<WorldHealthBar>();
        bar.offset = new Vector3(0f, healthBarHeight, 0f);
        bar.borderColor = new Color(0.1f, 0.35f, 0.85f);
        bar.Initialize(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
