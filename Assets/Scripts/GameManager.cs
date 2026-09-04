// GameManager.cs
using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public TerrainGenerator terrainGenerator;
    public GameObject towerPrefab;
    public EnemySpawner enemySpawner;

    [Header("Economy")]
    public int startingResources = 100;
    public int resourcesPerKill = 15;

    public static event Action<int> OnResourcesChanged;
    public static event Action OnGameOver;
    public static event Action OnGameRestarted;

    private GameObject spawnedTower;
    private int resources;
    private bool gameOver;

    public bool IsGameOver => gameOver;

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

    public void StartNewGame()
    {
        gameOver = false;
        Time.timeScale = 1f;

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
        resources += resourcesPerKill;
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
    }
}