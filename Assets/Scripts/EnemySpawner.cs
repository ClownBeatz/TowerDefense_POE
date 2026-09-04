// EnemySpawner.cs
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public TerrainGenerator terrainGenerator;
    public GameObject enemyPrefab;
    public float spawnInterval = 3f;

    private List<float> timers = new List<float>();
    private bool spawning;

    public void BeginSpawning()
    {
        timers.Clear();
        foreach (List<Vector3> path in terrainGenerator.Paths)
            timers.Add(Random.Range(0f, spawnInterval));

        spawning = true;
    }

    public void StopSpawning()
    {
        spawning = false;
    }

    private void Update()
    {
        if (!spawning) return;

        for (int i = 0; i < terrainGenerator.Paths.Count; i++)
        {
            timers[i] -= Time.deltaTime;

            if (timers[i] <= 0f)
            {
                SpawnEnemy(terrainGenerator.Paths[i]);
                timers[i] = spawnInterval;
            }
        }
    }

    private void SpawnEnemy(List<Vector3> path)
    {
        GameObject enemyObject = Instantiate(enemyPrefab, path[0], Quaternion.identity);
        Enemy enemy = enemyObject.GetComponent<Enemy>();

        if (enemy != null)
            enemy.SetPath(path);
    }
}