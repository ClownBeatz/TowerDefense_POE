using UnityEngine;

/// <summary>Tanky enemy that smashes defenders.</summary>
public class BruteEnemy : Enemy
{
    [Header("Brute")]
    public Color slamColor = new Color(0.75f, 0.3f, 1f);

    // Defenders first; only go for the tower when no defender is in reach.
    protected override Collider FindTarget()
    {
        Collider defender = Targeting.FindClosest(transform.position, attackRange, "Defender");
        return defender != null ? defender : Targeting.FindClosest(transform.position, attackRange, "Tower");
    }

    protected override void Attack(Collider target)
    {
        foreach (IDamageable victim in Targeting.FindAll(transform.position, attackRange, "Defender", "Tower"))
            victim.TakeDamage(damage);

        PulseRing.Spawn(transform.position - Vector3.up * heightOffset, attackRange, slamColor, 0.5f);
    }
}
