using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Places defenders on buildable spots.</summary>
public class DefenderPlacer : MonoBehaviour
{
    public GameManager gameManager;
    public TerrainGenerator terrainGenerator;
    public List<Defender> defenderPrefabs = new List<Defender>();
    public float placementRadius = 1.5f;

    [Header("Tile Highlight")]
    public Color validColor = new Color(0.4f, 1f, 0.5f);
    public Color invalidColor = new Color(1f, 0.3f, 0.3f);

    [Header("Range Preview")]
    public float rangeRingHeight = 2f;
    public int rangeRingSegments = 48;

    public event Action<Defender> OnSelectionChanged;

    private Renderer highlightedRenderer;
    private Color highlightedOriginalColor;
    private LineRenderer rangeRing;
    private readonly Dictionary<Defender, Vector3> placedSpots = new Dictionary<Defender, Vector3>();

    public int SelectedIndex { get; private set; }
    public Defender Selected => defenderPrefabs.Count > 0 ? defenderPrefabs[SelectedIndex] : null;

    private void Awake()
    {
        BuildRangeRing();
    }

    private void OnEnable()
    {
        Defender.OnDefenderDestroyed += FreeSpot;
        GameManager.OnGameRestarted += placedSpots.Clear;
    }

    private void OnDisable()
    {
        Defender.OnDefenderDestroyed -= FreeSpot;
        GameManager.OnGameRestarted -= placedSpots.Clear;
    }

    public void SelectDefender(int index)
    {
        if (index < 0 || index >= defenderPrefabs.Count) return;

        SelectedIndex = index;
        OnSelectionChanged?.Invoke(Selected);
    }

    private void BuildRangeRing()
    {
        GameObject ringObject = new GameObject("RangePreview");
        ringObject.transform.SetParent(transform);
        rangeRing = ringObject.AddComponent<LineRenderer>();
        rangeRing.useWorldSpace = true;
        rangeRing.loop = true;
        rangeRing.widthMultiplier = 0.08f;
        rangeRing.material = new Material(Shader.Find("Sprites/Default"));
        rangeRing.startColor = new Color(1f, 1f, 1f, 0.55f);
        rangeRing.endColor = new Color(1f, 1f, 1f, 0.55f);
        ringObject.SetActive(false);
    }

    private void Update()
    {
        HandleHotkeys();
        UpdateHoverPreview();

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            TryPlaceDefender();
    }

    private void HandleHotkeys()
    {
        for (int i = 0; i < defenderPrefabs.Count && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SelectDefender(i);
        }
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void UpdateHoverPreview()
    {
        ClearHighlight();

        if (Camera.main == null || Selected == null || IsPointerOverUI())
        {
            rangeRing.gameObject.SetActive(false);
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 200f) || !TryFindNearbySpot(hit.point, out Vector3 spot))
        {
            rangeRing.gameObject.SetActive(false);
            return;
        }

        bool affordable = gameManager.CanAfford(Selected.cost);

        if (terrainGenerator.BuildableBlocks.TryGetValue(spot, out GameObject block))
        {
            highlightedRenderer = block.GetComponent<Renderer>();
            if (highlightedRenderer != null)
            {
                highlightedOriginalColor = highlightedRenderer.material.color;
                highlightedRenderer.material.color = affordable ? validColor : invalidColor;
            }
        }

        DrawRing(spot, Selected.attackRange);
    }

    private void DrawRing(Vector3 center, float radius)
    {
        int segments = Mathf.Max(rangeRingSegments, 3);

        rangeRing.gameObject.SetActive(true);
        rangeRing.positionCount = segments;

        float ringY = center.y + rangeRingHeight;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 point = new Vector3(center.x + Mathf.Cos(angle) * radius, ringY, center.z + Mathf.Sin(angle) * radius);
            rangeRing.SetPosition(i, point);
        }
    }

    private void ClearHighlight()
    {
        if (highlightedRenderer != null)
        {
            highlightedRenderer.material.color = highlightedOriginalColor;
            highlightedRenderer = null;
        }
    }

    private void TryPlaceDefender()
    {
        if (Camera.main == null || Selected == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return;
        if (!TryFindNearbySpot(hit.point, out Vector3 spot)) return;
        if (!gameManager.TrySpendResources(Selected.cost)) return;

        ClearHighlight();
        terrainGenerator.BuildableSpots.Remove(spot);

        Defender placed = Instantiate(Selected, spot, Quaternion.identity);
        placedSpots[placed] = spot;
    }

    // When a defender is destroyed its spot opens up again so the player can rebuild.
    private void FreeSpot(Defender defender)
    {
        if (!placedSpots.TryGetValue(defender, out Vector3 spot)) return;

        placedSpots.Remove(defender);
        terrainGenerator.BuildableSpots.Add(spot);
    }

    private bool TryFindNearbySpot(Vector3 point, out Vector3 result)
    {
        result = Vector3.zero;
        float closestDist = placementRadius;
        bool found = false;

        foreach (Vector3 spot in terrainGenerator.BuildableSpots)
        {
            float dist = Vector3.Distance(new Vector3(point.x, spot.y, point.z), spot);
            if (dist < closestDist)
            {
                closestDist = dist;
                result = spot;
                found = true;
            }
        }

        return found;
    }
}
