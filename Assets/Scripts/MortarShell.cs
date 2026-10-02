using UnityEngine;

/// <summary>Arcing shell that deals splash damage.</summary>
public class MortarShell : MonoBehaviour
{
    public float flightTime = 0.9f;
    public float arcHeight = 4f;
    public Color blastColor = new Color(1f, 0.55f, 0.1f);

    private Vector3 start;
    private Vector3 end;
    private float damage;
    private float radius;
    private float elapsed;

    public void Launch(Vector3 targetPosition, float damageAmount, float blastRadius)
    {
        start = transform.position;
        end = targetPosition;
        damage = damageAmount;
        radius = blastRadius;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightTime);

        // 4t(1-t) peaks at 1 halfway through the flight.
        transform.position = Vector3.Lerp(start, end, t) + Vector3.up * (arcHeight * 4f * t * (1f - t));

        if (t >= 1f)
            Explode();
    }

    private void Explode()
    {
        foreach (IDamageable enemy in Targeting.FindAll(end, radius, "Enemy"))
            enemy.TakeDamage(damage);

        PulseRing.Spawn(end, radius, blastColor);
        Destroy(gameObject);
    }
}
