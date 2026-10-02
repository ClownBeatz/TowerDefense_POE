using System;
using UnityEngine;

/// <summary>Runs the main game loop.</summary>
public class GameManager : MonoBehaviour
{
    [Header("References")]
    public TerrainGenerator terrainGenerator;
    public GameObject towerPrefab;
    public EnemySpawner enemySpawner;

    [Header("Economy")]
    public int startingResources = 100;

    public static event Action<int> OnResourcesChanged;
    public static event Action<bool> OnPauseChanged;
    public static event Action OnGameOver;
    public static event Action OnGameRestarted;

    private GameObject spawnedTower;
    private int resources;
    private bool gameOver;

    public bool IsGameOver => gameOver;
    public bool IsPaused => Time.timeScale == 0f && !gameOver;
    public int CurrentResources => resources;

    private void Start()
    {
        StartNewGame();
    }

    private void OnEnable()
    {
        Tower.OnTowerDestroyed += HandleGameOver;
        Enemy.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        Tower.OnTowerDestroyed -= HandleGameOver;
        Enemy.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void StartNewGame()
    {
        gameOver = false;
        Time.timeScale = 1f;
        OnPauseChanged?.Invoke(false);

        if (spawnedTower != null)
            Destroy(spawnedTower);

        ClearBattlefield();

        terrainGenerator.GenerateTerrain();
        spawnedTower = Instantiate(towerPrefab, terrainGenerator.TowerPosition, Quaternion.identity);

        resources = startingResources;
        OnResourcesChanged?.Invoke(resources);

        enemySpawner.BeginSpawning();
        OnGameRestarted?.Invoke();
    }

    private void ClearBattlefield()
    {
        foreach (GameObject defender in GameObject.FindGameObjectsWithTag("Defender"))
            Destroy(defender);

        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
            Destroy(enemy);
    }

    public bool TrySpendResources(int amount)
    {
        if (resources < amount) return false;

        resources -= amount;
        OnResourcesChanged?.Invoke(resources);
        return true;
    }

    public bool CanAfford(int amount) => resources >= amount;

    private void HandleEnemyKilled(Enemy enemy)
    {
        resources += enemy.reward;
        OnResourcesChanged?.Invoke(resources);
    }

    private void HandleGameOver()
    {
        if (gameOver) return;

        gameOver = true;
        Time.timeScale = 0f;
        enemySpawner.StopSpawning();
        OnGameOver?.Invoke();
    }

    public void TogglePause()
    {
        if (gameOver) return;

        Time.timeScale = Time.timeScale > 0f ? 0f : 1f;
        OnPauseChanged?.Invoke(IsPaused);
    }
}
