using UnityEngine;

/// <summary>Expanding ring effect for area attacks.</summary>
public class PulseRing : MonoBehaviour
{
    private LineRenderer ring;
    private Color color;
    private float maxRadius;
    private float duration;
    private float elapsed;

    public static void Spawn(Vector3 position, float radius, Color color, float duration = 0.4f)
    {
        GameObject ringObject = new GameObject("PulseRing");
        ringObject.transform.position = position + Vector3.up * 0.1f;

        PulseRing pulse = ringObject.AddComponent<PulseRing>();
        pulse.ring = RingVisual.Create(ringObject.transform, color, 0.15f);
        pulse.color = color;
        pulse.maxRadius = radius;
        pulse.duration = duration;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        RingVisual.SetCircle(ring, Vector3.zero, Mathf.Lerp(0.2f, maxRadius, t));

        Color faded = new Color(color.r, color.g, color.b, 1f - t);
        ring.startColor = faded;
        ring.endColor = faded;

        if (t >= 1f)
            Destroy(gameObject);
    }
}

/// <summary>Draws circles with a LineRenderer.</summary>
public static class RingVisual
{
    private const int Segments = 40;
    private static Material sharedMaterial;

    public static LineRenderer Create(Transform parent, Color color, float width)
    {
        if (sharedMaterial == null)
            sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        GameObject ringObject = parent.gameObject;
        LineRenderer ring = ringObject.GetComponent<LineRenderer>();
        if (ring == null)
            ring = ringObject.AddComponent<LineRenderer>();

        ring.useWorldSpace = false;
        ring.loop = true;
        ring.widthMultiplier = width;
        ring.material = sharedMaterial;
        ring.startColor = color;
        ring.endColor = color;
        ring.positionCount = Segments;
        return ring;
    }

    public static void SetCircle(LineRenderer ring, Vector3 localCenter, float radius)
    {
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            ring.SetPosition(i, localCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }
}
