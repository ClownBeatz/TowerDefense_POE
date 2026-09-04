// Projectile.cs
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 18f;
    public float hitDistance = 0.15f;
    public Color trailColor = new Color(1f, 0.85f, 0.3f);

    private Transform target;
    private IDamageable targetDamageable;
    private float damage;
    private Vector3 lastKnownPosition;
    private TrailRenderer trail;

    private void Awake()
    {
        SetupTrail();
    }

    private void SetupTrail()
    {
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.15f;
        trail.startWidth = 0.08f;
        trail.endWidth = 0.01f;
        trail.minVertexDistance = 0.03f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = trailColor;
        trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
    }

    public void Launch(Transform targetTransform, IDamageable damageable, float damageAmount)
    {
        target = targetTransform;
        targetDamageable = damageable;
        damage = damageAmount;
        lastKnownPosition = targetTransform != null ? targetTransform.position : transform.position;
    }

    private void Update()
    {
        Vector3 targetPos = target != null ? target.position : lastKnownPosition;
        lastKnownPosition = targetPos;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        Vector3 toTarget = targetPos - transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
            transform.forward = toTarget.normalized;

        if (Vector3.Distance(transform.position, targetPos) < hitDistance)
            Impact();
    }

    private void Impact()
    {
        if (target != null && targetDamageable != null && !targetDamageable.IsDead)
            targetDamageable.TakeDamage(damage);

        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        enabled = false;
        Destroy(gameObject, trail != null ? trail.time : 0f);
    }
}