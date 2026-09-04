// Defender.cs
using UnityEngine;

public class Defender : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 40f;

    [Header("Attack")]
    public float attackRange = 6f;
    public float damage = 12f;
    public float attacksPerSecond = 1.5f;
    public GameObject projectilePrefab;

    [Header("Positioning")]
    public float heightOffset = 1f;

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
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        Collider closest = null;
        float closestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

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
            Destroy(gameObject);
    }

    private void SpawnHealthBar()
    {
        GameObject barObject = new GameObject("HealthBar");
        barObject.transform.SetParent(transform);
        WorldHealthBar bar = barObject.AddComponent<WorldHealthBar>();
        bar.Initialize(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}