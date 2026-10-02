using UnityEngine;
using UnityEngine.UI;

/// <summary>Health bar above a unit.</summary>
[RequireComponent(typeof(Canvas))]
public class WorldHealthBar : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 1.3f, 0f);
    public Vector2 size = new Vector2(1.2f, 0.2f);
    public Color borderColor = new Color(0f, 0f, 0f, 0.9f);
    public Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    public Color chipColor = new Color(1f, 0.9f, 0.6f);

    private IDamageable target;
    private HealthBarFill bar;
    private Camera cam;

    public void Initialize(IDamageable damageable)
    {
        target = damageable;
        BuildBar();
    }

    private void BuildBar()
    {
        cam = Camera.main;

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        transform.localPosition = offset;

        RectTransform rect = GetComponent<RectTransform>();
        rect.sizeDelta = size * 100f;
        transform.localScale = Vector3.one * 0.01f;

        if (cam != null)
            transform.rotation = cam.transform.rotation;

        Image border = CreateLayer("Border", borderColor);
        border.rectTransform.offsetMin = new Vector2(-5f, -5f);
        border.rectTransform.offsetMax = new Vector2(5f, 5f);

        CreateLayer("Background", backgroundColor);

        bar = gameObject.AddComponent<HealthBarFill>();
        bar.chip = CreateLayer("Chip", chipColor);
        bar.chip.type = Image.Type.Filled;
        bar.chip.fillMethod = Image.FillMethod.Horizontal;
        bar.fill = CreateLayer("Fill", Color.white);
        bar.fill.type = Image.Type.Filled;
        bar.fill.fillMethod = Image.FillMethod.Horizontal;

        bar.SetValue(CurrentPercent(), true);
    }

    private Image CreateLayer(string layerName, Color color)
    {
        GameObject layer = new GameObject(layerName, typeof(Image));
        layer.transform.SetParent(transform, false);

        Image image = layer.GetComponent<Image>();
        image.color = color;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return image;
    }

    private float CurrentPercent()
    {
        return target.MaxHealth > 0f ? target.CurrentHealth / target.MaxHealth : 0f;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (cam == null)
            cam = Camera.main;

        if (cam != null)
            transform.rotation = cam.transform.rotation;

        bar.SetValue(CurrentPercent());
    }
}
