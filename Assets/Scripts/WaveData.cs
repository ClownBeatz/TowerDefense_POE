using System.Collections.Generic;
using System.Linq;

/// <summary>One enemy to spawn.</summary>
public class SpawnOrder
{
    public Enemy Prefab;
    public int PathIndex;
}

/// <summary>The planned contents of a wave.</summary>
public class WavePlan
{
    public int Wave;
    public int Budget;
    public float HealthMultiplier;
    public float SpawnGap;
    public bool IsSurge;
    public readonly List<SpawnOrder> Spawns = new List<SpawnOrder>();
    public readonly List<string> Reasons = new List<string>();

    public WavePlan(int wave)
    {
        Wave = wave;
    }

    public float SpawnDuration => Spawns.Count * SpawnGap;

    public string MixSummary()
    {
        return string.Join(", ", Spawns
            .GroupBy(s => s.Prefab.type)
            .Select(g => g.Count() + " " + g.Key));
    }
}

/// <summary>Results of a finished wave.</summary>
public class WaveReport
{
    public int Wave;
    public float TowerDamagePercent;
    public float TowerHealthPercent;
    public float ClearTime;
    public float AverageKillProgress;
    public int EnemiesKilled;
    public int DefendersLost;
    public int ResourcesSpent;
    public int ResourcesBanked;
    public bool IsHoarding;
}
