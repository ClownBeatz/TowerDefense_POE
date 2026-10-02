using System.Collections.Generic;
using UnityEngine;

/// <summary>Tracks how the player is doing each wave.</summary>
public class PlayerTracker : MonoBehaviour
{
    public GameManager gameManager;
    public TerrainGenerator terrainGenerator;

    [Header("Play Style Thresholds")]
    [Tooltip("Share of total defence value one defender type needs before it counts as the player's favourite.")]
    [Range(0f, 1f)] public float favouriteShare = 0.5f;
    public int minDefendersForFavourite = 3;
    [Tooltip("Unspent resources at wave end that count as hoarding (if spots are still free).")]
    public int hoardThreshold = 80;

    private float towerHealth = -1f;
    private float towerMaxHealth;
    private float towerDamageThisWave;
    private int defendersLostThisWave;
    private int resourcesSpentThisWave;
    private int lastResources;
    private float killProgressSum;
    private int killsThisWave;
    private float waveStartTime;

    private void OnEnable()
    {
        Tower.OnTowerHealthChanged += HandleTowerHealthChanged;
        Defender.OnDefenderDestroyed += HandleDefenderDestroyed;
        Enemy.OnEnemyKilled += HandleEnemyKilled;
        GameManager.OnResourcesChanged += HandleResourcesChanged;
    }

    private void OnDisable()
    {
        Tower.OnTowerHealthChanged -= HandleTowerHealthChanged;
        Defender.OnDefenderDestroyed -= HandleDefenderDestroyed;
        Enemy.OnEnemyKilled -= HandleEnemyKilled;
        GameManager.OnResourcesChanged -= HandleResourcesChanged;
    }

    public void ResetForNewGame()
    {
        towerHealth = -1f;
        lastResources = gameManager.CurrentResources;
        BeginWave();
    }

    public void BeginWave()
    {
        towerDamageThisWave = 0f;
        defendersLostThisWave = 0;
        resourcesSpentThisWave = 0;
        killProgressSum = 0f;
        killsThisWave = 0;
        waveStartTime = Time.time;
    }

    public WaveReport EndWave(int wave)
    {
        int banked = gameManager.CurrentResources;
        bool spotsFree = terrainGenerator.BuildableSpots.Count > 0;

        return new WaveReport
        {
            Wave = wave,
            TowerDamagePercent = towerMaxHealth > 0f ? towerDamageThisWave / towerMaxHealth : 0f,
            TowerHealthPercent = towerMaxHealth > 0f ? towerHealth / towerMaxHealth : 1f,
            ClearTime = Time.time - waveStartTime,
            AverageKillProgress = killsThisWave > 0 ? killProgressSum / killsThisWave : 0f,
            EnemiesKilled = killsThisWave,
            DefendersLost = defendersLostThisWave,
            ResourcesSpent = resourcesSpentThisWave,
            ResourcesBanked = banked,
            IsHoarding = spotsFree && resourcesSpentThisWave == 0 && banked >= hoardThreshold
        };
    }

    // Which defender type makes up most of the player's defence, weighted by cost.
    public bool TryGetFavouriteDefender(out DefenderType favourite, out float share)
    {
        favourite = DefenderType.Gunner;
        share = 0f;

        Defender[] defenders = FindObjectsByType<Defender>(FindObjectsSortMode.None);
        if (defenders.Length < minDefendersForFavourite) return false;

        Dictionary<DefenderType, float> valueByType = new Dictionary<DefenderType, float>();
        float totalValue = 0f;

        foreach (Defender defender in defenders)
        {
            valueByType.TryGetValue(defender.type, out float value);
            valueByType[defender.type] = value + defender.cost;
            totalValue += defender.cost;
        }

        foreach (KeyValuePair<DefenderType, float> entry in valueByType)
        {
            float typeShare = entry.Value / totalValue;
            if (typeShare > share)
            {
                share = typeShare;
                favourite = entry.Key;
            }
        }

        return share >= favouriteShare;
    }

    // Each defender adds its cost times the fraction of the path it reaches, so a
    // defender by the tower (where all paths meet) only counts a little for each.
    public float[] GetPathDefence(List<List<Vector3>> paths)
    {
        float[] scores = new float[paths.Count];
        Defender[] defenders = FindObjectsByType<Defender>(FindObjectsSortMode.None);

        for (int i = 0; i < paths.Count; i++)
        {
            foreach (Defender defender in defenders)
                scores[i] += defender.cost * Coverage(defender, paths[i]);
        }

        return scores;
    }

    private static float Coverage(Defender defender, List<Vector3> path)
    {
        Vector2 position = new Vector2(defender.transform.position.x, defender.transform.position.z);
        int covered = 0;

        foreach (Vector3 waypoint in path)
        {
            if (Vector2.Distance(position, new Vector2(waypoint.x, waypoint.z)) <= defender.attackRange)
                covered++;
        }

        return (float)covered / path.Count;
    }

    private void HandleTowerHealthChanged(float current, float max)
    {
        // A fresh tower (new game) reports full health: just take it as the baseline.
        if (towerHealth >= 0f && current < towerHealth)
            towerDamageThisWave += towerHealth - current;

        towerHealth = current;
        towerMaxHealth = max;
    }

    private void HandleDefenderDestroyed(Defender defender)
    {
        defendersLostThisWave++;
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        killProgressSum += enemy.PathProgress;
        killsThisWave++;
    }

    private void HandleResourcesChanged(int amount)
    {
        if (amount < lastResources)
            resourcesSpentThisWave += lastResources - amount;

        lastResources = amount;
    }
}
