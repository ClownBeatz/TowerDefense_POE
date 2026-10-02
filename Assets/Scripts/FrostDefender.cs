using UnityEngine;

/// <summary>Slows and damages enemies in range.</summary>
public class FrostDefender : Defender
{
    [Header("Frost")]
    [Range(0f, 1f)] public float slowStrength = 0.5f;
    public float slowDuration = 2f;
    public Color pulseColor = new Color(0.45f, 0.9f, 1f);
    public Transform crystal;
    public float crystalSpinSpeed = 90f;

    protected override void Update()
    {
        base.Update();

        if (crystal != null)
            crystal.Rotate(Vector3.up, crystalSpinSpeed * Time.deltaTime, Space.World);
    }

    // The target only tells us an enemy is in range; the pulse hits all of them.
    protected override void Attack(Collider target)
    {
        foreach (IDamageable victim in Targeting.FindAll(transform.position, attackRange, "Enemy"))
        {
            victim.TakeDamage(damage);
            (victim as Enemy)?.ApplySlow(slowStrength, slowDuration);
        }

        PulseRing.Spawn(transform.position - Vector3.up * heightOffset, attackRange, pulseColor);
    }
}
