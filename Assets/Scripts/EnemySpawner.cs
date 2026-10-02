using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>Spawns enemy waves and scales difficulty.</summary>
public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    public class EnemyOption
    {
        public Enemy prefab;
        public int cost = 2;
        public int unlockWave = 1;
        public float baseWeight = 1f;
    }

    [Header("References")]
    public TerrainGenerator terrainGenerator;
    public PlayerTracker tracker;
    public List<EnemyOption> enemyOptions = new List<EnemyOption>();

    [Header("Budget Scaling")]
    public int baseBudget = 10;
    public int budgetPerWave = 4;
    public float healthGrowthPerWave = 0.06f;

    [Header("Pacing")]
    public float firstWaveDelay = 15f;
    public float timeBetweenWaves = 10f;
    public int surgeEvery = 5;
    public float surgeBudgetMultiplier = 1.3f;
    public float breatherBudgetMultiplier = 0.85f;

    [Header("Spawn Timing")]
    public float slowestSpawnGap = 1.2f;
    public float fastestSpawnGap = 0.45f;

    [Header("Adaptive Difficulty")]
    public float startingDifficulty = 1f;
    public float minDifficulty = 0.6f;
    public float maxDifficulty = 1.8f;
    public float counterWeightMultiplier = 3f;
    [Tooltip("Defence score that counts as one 'unit' when weighting paths.")]
    public float defencePerUnit = 5f;

    public static event Action<WavePlan> OnWaveStarted;
    public static event Action<WaveReport> OnWaveCompleted;

    private readonly List<Enemy> aliveEnemies = new List<Enemy>();
    private readonly List<GameObject> pathMarkers = new List<GameObject>();
    private int pendingSpawns;
    private float typicalTravelTime;

    public int CurrentWave { get; private set; }
    public float Difficulty { get; private set; }
    public float Countdown { get; private set; }
    public float CurrentIntermission { get; private set; }
    public bool WaveInProgress { get; private set; }
    public WavePlan CurrentPlan { get; private set; }
    public WaveReport LastReport { get; private set; }
    public List<string> LastAdjustments { get; } = new List<string>();
    public int EnemiesRemaining => aliveEnemies.Count(e => e != null) + pendingSpawns;

    public string SkillLabel =>
        Difficulty < 0.85f ? "Struggling" :
        Difficulty > 1.15f ? "Cruising" : "Balanced";

    public void BeginSpawning()
    {
        StopAllCoroutines();
        aliveEnemies.Clear();
        LastAdjustments.Clear();
        CurrentWave = 0;
        CurrentPlan = null;
        LastReport = null;
        WaveInProgress = false;
        pendingSpawns = 0;
        Difficulty = startingDifficulty;

        // Rough time for a Grunt to walk a path, used to judge how fast a wave was cleared.
        float averagePathLength = (float)terrainGenerator.Paths.Average(PathLength);
        typicalTravelTime = averagePathLength / enemyOptions[0].prefab.moveSpeed;

        tracker.ResetForNewGame();
        CreatePathMarkers();
        StartCoroutine(RunWaves());
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
        WaveInProgress = false;
    }

    private IEnumerator RunWaves()
    {
        float delay = firstWaveDelay;

        while (true)
        {
            CurrentIntermission = delay;
            for (Countdown = delay; Countdown > 0f; Countdown -= Time.deltaTime)
                yield return null;

            CurrentWave++;
            CurrentPlan = BuildWavePlan(CurrentWave);
            tracker.BeginWave();
            WaveInProgress = true;
            OnWaveStarted?.Invoke(CurrentPlan);

            yield return SpawnWave(CurrentPlan);
            yield return new WaitUntil(() => EnemiesRemaining == 0);

            WaveInProgress = false;
            LastReport = tracker.EndWave(CurrentWave);
            delay = AdaptToPlayer(LastReport, CurrentPlan);
            OnWaveCompleted?.Invoke(LastReport);
        }
    }

    private IEnumerator SpawnWave(WavePlan plan)
    {
        aliveEnemies.Clear();
        pendingSpawns = plan.Spawns.Count;

        foreach (SpawnOrder order in plan.Spawns)
        {
            List<Vector3> path = terrainGenerator.Paths[order.PathIndex];
            Enemy enemy = Instantiate(order.Prefab, path[0], Quaternion.identity);
            enemy.SetPath(path);
            enemy.ScaleHealth(plan.HealthMultiplier);
            aliveEnemies.Add(enemy);
            pendingSpawns--;

            yield return new WaitForSeconds(plan.SpawnGap);
        }
    }

    //Planning a wave

    private WavePlan BuildWavePlan(int wave)
    {
        WavePlan plan = new WavePlan(wave);

        int baseForWave = baseBudget + budgetPerWave * (wave - 1);
        float budget = baseForWave * Difficulty;
        plan.Reasons.Add($"Budget {baseForWave} x {Difficulty:0.00} difficulty");

        if (wave % surgeEvery == 0)
        {
            budget *= surgeBudgetMultiplier;
            plan.IsSurge = true;
            plan.Reasons.Add($"Surge wave: budget x{surgeBudgetMultiplier}");
        }
        else if (wave > surgeEvery && wave % surgeEvery == 1)
        {
            budget *= breatherBudgetMultiplier;
            plan.Reasons.Add($"Breather after surge: budget x{breatherBudgetMultiplier}");
        }

        plan.Budget = Mathf.Max(Mathf.RoundToInt(budget), enemyOptions[0].cost);
        plan.HealthMultiplier = 1f + healthGrowthPerWave * (wave - 1);
        plan.SpawnGap = Mathf.Lerp(slowestSpawnGap, fastestSpawnGap, Mathf.InverseLerp(minDifficulty, maxDifficulty, Difficulty));

        Dictionary<EnemyOption, float> weights = BuildEnemyWeights(plan);
        List<Enemy> picks = SpendBudget(plan, weights);

        // Slowest first, fastest last, so the whole wave reaches the defences together.
        picks.Sort((a, b) => a.moveSpeed.CompareTo(b.moveSpeed));
        AssignPaths(plan, picks);

        return plan;
    }

    private Dictionary<EnemyOption, float> BuildEnemyWeights(WavePlan plan)
    {
        Dictionary<EnemyOption, float> weights = new Dictionary<EnemyOption, float>();

        foreach (EnemyOption option in enemyOptions)
        {
            if (plan.Wave >= option.unlockWave)
                weights[option] = option.baseWeight;
        }

        if (!tracker.TryGetFavouriteDefender(out DefenderType favourite, out float share))
            return weights;

        EnemyType counter = CounterFor(favourite);
        EnemyOption counterOption = weights.Keys.FirstOrDefault(o => o.prefab.type == counter);
        if (counterOption == null)
            return weights;

        weights[counterOption] *= counterWeightMultiplier;
        plan.Reasons.Add($"You rely on {favourite}s ({share:P0}) so more {counter}s");

        // A swarm only works as a counter if it arrives densely.
        if (counter == EnemyType.Grunt)
            plan.SpawnGap *= 0.75f;

        return weights;
    }

    // Each defender type has a weakness the director can exploit.
    private static EnemyType CounterFor(DefenderType defender)
    {
        return defender switch
        {
            DefenderType.Gunner => EnemyType.Grunt,  // single target struggles with swarms
            DefenderType.Mortar => EnemyType.Runner, // slow shells can't keep up with fast runners
            DefenderType.Frost => EnemyType.Brute,   // brutes shrug off slows and smash defenders
            _ => EnemyType.Grunt
        };
    }

    private List<Enemy> SpendBudget(WavePlan plan, Dictionary<EnemyOption, float> weights)
    {
        List<Enemy> picks = new List<Enemy>();
        int remaining = plan.Budget;

        // A newly unlocked enemy always shows up on its unlock wave.
        foreach (EnemyOption option in weights.Keys)
        {
            if (option.unlockWave == plan.Wave && option.unlockWave > 1 && remaining >= option.cost)
            {
                picks.Add(option.prefab);
                remaining -= option.cost;
                plan.Reasons.Add($"New enemy unlocked: {option.prefab.type}");
            }
        }

        while (true)
        {
            List<EnemyOption> affordable = weights.Keys.Where(o => o.cost <= remaining).ToList();
            if (affordable.Count == 0) break;

            EnemyOption choice = WeightedPick(affordable, o => weights[o]);
            picks.Add(choice.prefab);
            remaining -= choice.cost;
        }

        return picks;
    }

    // Brutes go where the defence is strongest to break it, Runners rush the
    // weakest path, Grunts lean toward lightly defended paths.
    private void AssignPaths(WavePlan plan, List<Enemy> picks)
    {
        float[] defence = tracker.GetPathDefence(terrainGenerator.Paths);
        int weakest = Array.IndexOf(defence, defence.Min());
        int strongest = Array.IndexOf(defence, defence.Max());
        bool evenlyDefended = defence.Max() - defence.Min() < defencePerUnit * 0.1f;

        List<int> pathIndices = Enumerable.Range(0, defence.Length).ToList();

        foreach (Enemy prefab in picks)
        {
            int path;
            if (evenlyDefended)
                path = Random.Range(0, defence.Length);
            else if (prefab.type == EnemyType.Runner)
                path = weakest;
            else if (prefab.type == EnemyType.Brute)
                path = strongest;
            else
                path = WeightedPick(pathIndices, i => 1f / (1f + defence[i] / defencePerUnit));

            plan.Spawns.Add(new SpawnOrder { Prefab = prefab, PathIndex = path });
        }

        if (evenlyDefended)
        {
            plan.Reasons.Add("Paths equally defended: spreading out");
            return;
        }

        plan.Reasons.Add($"Path {weakest + 1} weakest ({defence[weakest]:0.0}) gets Grunts/Runners");
        if (picks.Any(p => p.type == EnemyType.Brute))
            plan.Reasons.Add($"Brutes sent to break Path {strongest + 1} ({defence[strongest]:0.0})");
    }

    private static T WeightedPick<T>(List<T> items, Func<T, float> weightOf)
    {
        float total = items.Sum(weightOf);
        float roll = Random.value * total;

        foreach (T item in items)
        {
            roll -= weightOf(item);
            if (roll <= 0f) return item;
        }

        return items[items.Count - 1];
    }

    //Adapting after a wave

    // Nudges difficulty from how the wave went and returns the next intermission length.
    private float AdaptToPlayer(WaveReport report, WavePlan plan)
    {
        LastAdjustments.Clear();
        float change = 0f;

        if (report.TowerDamagePercent >= 0.2f)
            change += Adjust(-0.2f, $"Tower lost {report.TowerDamagePercent:P0} HP, easing off");
        else if (report.TowerDamagePercent > 0.05f)
            change += Adjust(-0.08f, $"Tower lost {report.TowerDamagePercent:P0} HP, slight ease");
        else if (report.TowerDamagePercent <= 0f)
            change += Adjust(0.1f, "No tower damage, pushing harder");

        if (report.EnemiesKilled > 0 && report.AverageKillProgress < 0.4f)
            change += Adjust(0.05f, $"Enemies died early ({report.AverageKillProgress:P0} along path)");
        else if (report.AverageKillProgress > 0.8f)
            change += Adjust(-0.05f, $"Enemies got deep ({report.AverageKillProgress:P0} along path)");

        float clearedAfterLastSpawn = report.ClearTime - plan.SpawnDuration;
        if (clearedAfterLastSpawn < typicalTravelTime * 0.5f)
            change += Adjust(0.05f, $"Wave cleared fast ({report.ClearTime:0}s)");

        if (report.DefendersLost >= 2)
            change += Adjust(-0.05f, $"Lost {report.DefendersLost} defenders");

        Difficulty = Mathf.Clamp(Difficulty + change, minDifficulty, maxDifficulty);

        float delay = timeBetweenWaves;

        if (report.TowerHealthPercent < 0.3f)
        {
            delay += 5f;
            LastAdjustments.Add("Tower critical: +5s to rebuild");
        }

        if (report.IsHoarding)
        {
            delay = Mathf.Max(delay - 5f, 4f);
            LastAdjustments.Add($"Banking {report.ResourcesBanked} unspent: next wave sooner");
        }

        return delay;
    }

    private float Adjust(float amount, string reason)
    {
        LastAdjustments.Add($"{reason} ({amount:+0.00;-0.00})");
        return amount;
    }

    //Helpers

    private static double PathLength(List<Vector3> path)
    {
        float length = 0f;
        for (int i = 1; i < path.Count; i++)
            length += Vector3.Distance(path[i - 1], path[i]);
        return length;
    }

    // Numbered labels at each path start so the director's decisions can be seen in-game.
    private void CreatePathMarkers()
    {
        foreach (GameObject marker in pathMarkers)
            Destroy(marker);
        pathMarkers.Clear();

        for (int i = 0; i < terrainGenerator.Paths.Count; i++)
        {
            Vector3 start = terrainGenerator.Paths[i][0];

            GameObject marker = new GameObject($"PathMarker {i + 1}");
            marker.transform.position = start + Vector3.up * 0.1f;
            RingVisual.SetCircle(RingVisual.Create(marker.transform, new Color(1f, 0.4f, 0.3f), 0.12f), Vector3.zero, 1.1f);

            GameObject label = new GameObject("Label");
            label.transform.SetParent(marker.transform, false);
            label.transform.localPosition = Vector3.up * 2.5f;
            if (Camera.main != null)
                label.transform.rotation = Camera.main.transform.rotation;

            TextMeshPro text = label.AddComponent<TextMeshPro>();
            text.text = "P" + (i + 1);
            text.fontSize = 12f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            pathMarkers.Add(marker);
        }
    }
}
