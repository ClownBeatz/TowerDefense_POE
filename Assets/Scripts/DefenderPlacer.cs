// DefenderPlacer.cs
using UnityEngine;

public class DefenderPlacer : MonoBehaviour
{
    public GameManager gameManager;
    public TerrainGenerator terrainGenerator;
    public GameObject defenderPrefab;
    public int defenderCost = 20;
    public float placementRadius = 1.5f;

    [Header("Tile Highlight")]
    public Color validColor = new Color(0.4f, 1f, 0.5f);
    public Color invalidColor = new Color(1f, 0.3f, 0.3f);

    [Header("Range Preview")]
    public float rangeRingHeight = 2f;
    public int rangeRingSegments = 48;

    private Renderer highlightedRenderer;
    private Color highlightedOriginalColor;
    private LineRenderer rangeRing;
    private float defenderRange;

    private void Awake()
    {
        if (defenderPrefab != null)
        {
            Defender d = defenderPrefab.GetComponent<Defender>();
            if (d != null) defenderRange = d.attackRange;
        }

        BuildRangeRing();
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
        UpdateHoverPreview();

        if (Input.GetMouseButtonDown(0))
            TryPlaceDefender();
    }

    private void UpdateHoverPreview()
    {
        ClearHighlight();

        if (Camera.main == null)
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

        bool affordable = gameManager.CanAfford(defenderCost);

        if (terrainGenerator.BuildableBlocks.TryGetValue(spot, out GameObject block))
        {
            highlightedRenderer = block.GetComponent<Renderer>();
            if (highlightedRenderer != null)
            {
                highlightedOriginalColor = highlightedRenderer.material.color;
                highlightedRenderer.material.color = affordable ? validColor : invalidColor;
            }
        }

        DrawRing(spot);
    }

    private void DrawRing(Vector3 center)
    {
        int segments = Mathf.Max(rangeRingSegments, 3);

        rangeRing.gameObject.SetActive(true);
        rangeRing.positionCount = segments;

        float ringY = center.y + rangeRingHeight;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 point = new Vector3(center.x + Mathf.Cos(angle) * defenderRange, ringY, center.z + Mathf.Sin(angle) * defenderRange);
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
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return;
        if (!TryFindNearbySpot(hit.point, out Vector3 spot)) return;
        if (!gameManager.TrySpendResources(defenderCost)) return;

        ClearHighlight();
        terrainGenerator.BuildableSpots.Remove(spot);
        Instantiate(defenderPrefab, spot, Quaternion.identity);
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