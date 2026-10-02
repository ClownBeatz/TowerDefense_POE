using UnityEngine;

/// <summary>Moves the camera and handles zoom.</summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public TerrainGenerator terrainGenerator;

    [Header("Angle")]
    [Range(30f, 85f)] public float pitch = 55f;

    [Header("Pan")]
    public float panSpeed = 25f;
    public bool edgePan = false;
    public float edgeSize = 12f;

    [Header("Zoom")]
    [Tooltip("How much each scroll tick zooms, as a fraction of the current distance.")]
    [Range(0.02f, 0.3f)] public float zoomStep = 0.12f;
    public float minDistance = 12f;

    [Header("Screen Kept Clear For The HUD")]
    [Tooltip("Fractions of the screen the map stays out of when fully zoomed out.")]
    [Range(0f, 0.4f)] public float sideMargin = 0.22f;
    [Range(0f, 0.4f)] public float topMargin = 0.14f;
    [Range(0f, 0.4f)] public float bottomMargin = 0.17f;

    // Path labels float above the spawn tiles, so the framing has to include them.
    private const float LabelHeight = 4f;

    private Camera cam;
    private Bounds mapBounds;
    private Vector3 focus;
    private Vector3 homeFocus;
    private float distance;
    private float maxDistance;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        GameManager.OnGameRestarted += FrameMap;
    }

    private void OnDisable()
    {
        GameManager.OnGameRestarted -= FrameMap;
    }

    public void FrameMap()
    {
        mapBounds = terrainGenerator.MapBounds;
        mapBounds.Encapsulate(mapBounds.max + Vector3.up * LabelHeight);
        focus = new Vector3(mapBounds.center.x, 0f, mapBounds.center.z);

        maxDistance = FindFitDistance();
        distance = maxDistance;
        CentreVertically();
        homeFocus = focus;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        ApplyTransform();
    }

    private void LateUpdate()
    {
        if (terrainGenerator.Paths == null) return;

        // The Game view can change size after the first frame (e.g. Maximize on Play).
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            FrameMap();

        // Unscaled time so the player can still look around while paused.
        float dt = Time.unscaledDeltaTime;

        // Zooming by a percentage feels the same whether you're close or far away.
        distance = Mathf.Clamp(distance * (1f - Input.mouseScrollDelta.y * zoomStep), minDistance, maxDistance);

        // Pan slower when zoomed in so it stays controllable.
        Vector3 move = ReadPanInput() * (panSpeed * (distance / maxDistance) * dt);

        // The pan area shrinks as you zoom out, so fully zoomed out is always the whole map.
        float zoomedIn = Mathf.InverseLerp(maxDistance, minDistance, distance);
        Vector3 reach = mapBounds.extents * zoomedIn;
        focus.x = Mathf.Clamp(focus.x + move.x, homeFocus.x - reach.x, homeFocus.x + reach.x);
        focus.z = Mathf.Clamp(focus.z + move.z, homeFocus.z - reach.z, homeFocus.z + reach.z);

        ApplyTransform();
    }

    private Vector3 ReadPanInput()
    {
        Vector3 input = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.z += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.z -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;

        if (edgePan && Application.isFocused)
        {
            Vector3 mouse = Input.mousePosition;
            bool onScreen = mouse.x >= 0f && mouse.y >= 0f && mouse.x <= Screen.width && mouse.y <= Screen.height;

            if (onScreen)
            {
                if (mouse.x < edgeSize) input.x -= 1f;
                if (mouse.x > Screen.width - edgeSize) input.x += 1f;
                if (mouse.y < edgeSize) input.z -= 1f;
                if (mouse.y > Screen.height - edgeSize) input.z += 1f;
            }
        }

        return Vector3.ClampMagnitude(input, 1f);
    }

    // Binary search for the closest distance where every corner of the map
    // lands inside the part of the screen not covered by HUD panels.
    private float FindFitDistance()
    {
        float near = minDistance;
        float far = 500f;

        for (int i = 0; i < 30; i++)
        {
            distance = (near + far) * 0.5f;
            CentreVertically();

            if (MapFitsOnScreen())
                far = distance;
            else
                near = distance;
        }

        return far;
    }

    // Perspective makes the near edge of the map look bigger, so slide the camera
    // forward/back until the map sits in the middle of the free screen space.
    private void CentreVertically()
    {
        float target = (bottomMargin + 1f - topMargin) * 0.5f;
        float low = mapBounds.min.z - mapBounds.size.z;
        float high = mapBounds.max.z + mapBounds.size.z;

        for (int i = 0; i < 20; i++)
        {
            focus.z = (low + high) * 0.5f;
            ApplyTransform();

            if (ViewportMiddleY() > target)
                low = focus.z;
            else
                high = focus.z;
        }
    }

    private float ViewportMiddleY()
    {
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Vector3 corner in MapCorners())
        {
            float y = cam.WorldToViewportPoint(corner).y;
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }

        return (minY + maxY) * 0.5f;
    }

    private Vector3[] MapCorners()
    {
        Vector3 min = mapBounds.min;
        Vector3 max = mapBounds.max;
        Vector3[] corners = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            corners[i] = new Vector3(
                (i & 1) == 0 ? min.x : max.x,
                (i & 2) == 0 ? min.y : max.y,
                (i & 4) == 0 ? min.z : max.z);
        }

        return corners;
    }

    private bool MapFitsOnScreen()
    {
        foreach (Vector3 corner in MapCorners())
        {
            Vector3 viewport = cam.WorldToViewportPoint(corner);
            if (viewport.z <= 0f ||
                viewport.x < sideMargin || viewport.x > 1f - sideMargin ||
                viewport.y < bottomMargin || viewport.y > 1f - topMargin)
                return false;
        }

        return true;
    }

    private void ApplyTransform()
    {
        Quaternion rotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
    }
}
