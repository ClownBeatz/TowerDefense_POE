using UnityEngine;

/// <summary>Fast enemy that shoots while running.</summary>
public class RunnerEnemy : Enemy
{
    [Header("Runner")]
    public Color trailColor = new Color(1f, 0.85f, 0.2f);

    protected override void Awake()
    {
        base.Awake();
        AddSpeedTrail();
    }

    // Never stops for defenders, only for the tower.
    protected override bool StopsToAttack(Collider target) => target.CompareTag("Tower");

    private void AddSpeedTrail()
    {
        TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.35f;
        trail.startWidth = 0.35f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = trailColor;
        trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
    }
}
