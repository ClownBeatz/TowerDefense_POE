using UnityEngine;

/// <summary>Fires splash shells at groups of enemies.</summary>
public class MortarDefender : Defender
{
    [Header("Mortar")]
    public MortarShell shellPrefab;
    public float splashRadius = 2.2f;

    protected override Collider FindTarget()
    {
        Collider best = null;
        int bestCount = 0;

        foreach (Collider candidate in Physics.OverlapSphere(transform.position, attackRange))
        {
            if (!candidate.CompareTag("Enemy")) continue;

            int clusterSize = Targeting.FindAll(candidate.transform.position, splashRadius, "Enemy").Count;
            if (clusterSize > bestCount)
            {
                bestCount = clusterSize;
                best = candidate;
            }
        }

        return best;
    }

    protected override void Attack(Collider target)
    {
        Enemy enemy = target.GetComponent<Enemy>();
        Vector3 aimPoint = target.transform.position;

        if (enemy != null)
            aimPoint += enemy.Velocity * shellPrefab.flightTime;

        MortarShell shell = Instantiate(shellPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        shell.Launch(aimPoint, damage, splashRadius);
    }
}
