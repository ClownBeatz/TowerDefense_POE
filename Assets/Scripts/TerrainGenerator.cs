// TerrainGenerator.cs
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Blocks")]
    public GameObject grassBlockPrefab;
    public GameObject pathBlockPrefab;

    [Header("Grid")]
    public int gridSize = 21;
    public float gridOffset = 2f;

    [Header("Paths")]
    public int numberOfPaths = 3;
    [Range(0f, 1f)] public float wanderChance = 0.25f;

    [Header("Buildable Spots")]
    public int buildableSampleInterval = 3;
    public int buildableOffset = 2;

    [Header("Height")]
    public float noiseHeight = 1.5f;
    public float noiseScale = 0.15f;

    private CellType[,] grid;
    private Vector2 noiseOffset;
    private List<GameObject> spawnedBlocks = new List<GameObject>();
    private HashSet<Vector2Int> buildableCells;

    public Vector3 TowerPosition { get; private set; }
    public List<List<Vector3>> Paths { get; private set; }
    public List<Vector3> BuildableSpots { get; private set; }
    public Dictionary<Vector3, GameObject> BuildableBlocks { get; private set; }

    private enum CellType { Grass, Path, Tower }

    public void GenerateTerrain()
    {
        foreach (GameObject block in spawnedBlocks)
            Destroy(block);
        spawnedBlocks.Clear();

        noiseOffset = new Vector2(Random.Range(0f, 9999f), Random.Range(0f, 9999f));
        grid = new CellType[gridSize, gridSize];

        Vector2Int center = new Vector2Int(gridSize / 2, gridSize / 2);
        grid[center.x, center.y] = CellType.Tower;

        Paths = new List<List<Vector3>>();
        List<Vector2Int> spawnPoints = ChooseSpawnPoints(numberOfPaths, center);
        List<List<Vector2Int>> gridPaths = new List<List<Vector2Int>>();

        foreach (Vector2Int spawn in spawnPoints)
        {
            List<Vector2Int> gridPath = WalkPath(spawn, center);
            gridPaths.Add(gridPath);

            List<Vector3> worldPath = new List<Vector3>();
            foreach (Vector2Int cell in gridPath)
            {
                if (grid[cell.x, cell.y] == CellType.Grass)
                    grid[cell.x, cell.y] = CellType.Path;

                worldPath.Add(GridToWorld(cell));
            }

            Paths.Add(worldPath);
        }

        buildableCells = new HashSet<Vector2Int>();
        foreach (List<Vector2Int> gridPath in gridPaths)
            CollectBuildableSpots(gridPath);

        BuildableSpots = new List<Vector3>();
        foreach (Vector2Int cell in buildableCells)
            BuildableSpots.Add(GridToWorld(cell));

        TowerPosition = GridToWorld(center);
        BuildableBlocks = new Dictionary<Vector3, GameObject>();

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (grid[x, z] == CellType.Tower) continue;

                Vector2Int cell = new Vector2Int(x, z);
                Vector3 worldPos = GridToWorld(cell);
                GameObject prefab = grid[x, z] == CellType.Path ? pathBlockPrefab : grassBlockPrefab;

                GameObject block = Instantiate(prefab, worldPos, Quaternion.identity);
                block.transform.SetParent(transform);
                spawnedBlocks.Add(block);

                if (grid[x, z] == CellType.Grass && buildableCells.Contains(cell))
                    BuildableBlocks[worldPos] = block;
            }
        }
    }

    private List<Vector2Int> ChooseSpawnPoints(int count, Vector2Int center)
    {
        List<Vector2Int> border = new List<Vector2Int>();

        for (int x = 0; x < gridSize; x++)
        {
            border.Add(new Vector2Int(x, 0));
            border.Add(new Vector2Int(x, gridSize - 1));
        }
        for (int z = 1; z < gridSize - 1; z++)
        {
            border.Add(new Vector2Int(0, z));
            border.Add(new Vector2Int(gridSize - 1, z));
        }

        List<Vector2Int> points = new List<Vector2Int>();
        int minSeparation = gridSize / (count + 1);
        int attempts = 0;

        while (points.Count < count && attempts < 1000)
        {
            Vector2Int candidate = border[Random.Range(0, border.Count)];
            bool farEnough = true;

            foreach (Vector2Int p in points)
            {
                if (Vector2Int.Distance(p, candidate) < minSeparation)
                {
                    farEnough = false;
                    break;
                }
            }

            if (farEnough)
                points.Add(candidate);

            attempts++;
        }

        while (points.Count < count)
            points.Add(border[Random.Range(0, border.Count)]);

        return points;
    }

    private List<Vector2Int> WalkPath(Vector2Int start, Vector2Int center)
    {
        List<Vector2Int> path = new List<Vector2Int> { start };
        Vector2Int current = start;
        int maxSteps = gridSize * 4;
        int steps = 0;

        while (current != center && steps < maxSteps)
        {
            int dx = center.x - current.x;
            int dz = center.y - current.y;
            Vector2Int move;

            if (Random.value < wanderChance)
            {
                move = Mathf.Abs(dx) > Mathf.Abs(dz)
                    ? new Vector2Int(0, Random.value < 0.5f ? 1 : -1)
                    : new Vector2Int(Random.value < 0.5f ? 1 : -1, 0);
            }
            else
            {
                move = Mathf.Abs(dx) > Mathf.Abs(dz)
                    ? new Vector2Int((int)Mathf.Sign(dx), 0)
                    : new Vector2Int(0, (int)Mathf.Sign(dz));
            }

            Vector2Int next = new Vector2Int(
                Mathf.Clamp(current.x + move.x, 0, gridSize - 1),
                Mathf.Clamp(current.y + move.y, 0, gridSize - 1));

            if (next != current)
            {
                current = next;
                path.Add(current);
            }

            steps++;
        }

        if (current != center)
            path.Add(center);

        return path;
    }

    private void CollectBuildableSpots(List<Vector2Int> path)
    {
        for (int i = 0; i < path.Count; i += buildableSampleInterval)
        {
            Vector2Int cell = path[i];
            Vector2Int neighbour = i < path.Count - 1 ? path[i + 1] : path[Mathf.Max(i - 1, 0)];
            Vector2Int forward = i < path.Count - 1 ? neighbour - cell : cell - neighbour;

            if (forward == Vector2Int.zero) continue;

            Vector2Int side = new Vector2Int(-forward.y, forward.x);

            TryAddBuildableCell(cell + side * buildableOffset);
            TryAddBuildableCell(cell - side * buildableOffset);
        }
    }

    private void TryAddBuildableCell(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= gridSize || cell.y < 0 || cell.y >= gridSize) return;
        if (grid[cell.x, cell.y] != CellType.Grass) return;

        buildableCells.Add(cell);
    }

    private Vector3 GridToWorld(Vector2Int cell)
    {
        float height = grid[cell.x, cell.y] == CellType.Grass
            ? Mathf.PerlinNoise((cell.x + noiseOffset.x) * noiseScale, (cell.y + noiseOffset.y) * noiseScale) * noiseHeight
            : 0f;

        return new Vector3(cell.x * gridOffset, height, cell.y * gridOffset);
    }

    private void OnDrawGizmosSelected()
    {
        if (Paths != null)
        {
            Gizmos.color = Color.yellow;
            foreach (List<Vector3> path in Paths)
            {
                for (int i = 0; i < path.Count - 1; i++)
                    Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }

        if (BuildableSpots != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Vector3 spot in BuildableSpots)
                Gizmos.DrawWireSphere(spot, 0.6f);
        }
    }
}