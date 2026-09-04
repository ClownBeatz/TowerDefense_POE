// WorldHealthBar.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class WorldHealthBar : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 1.3f, 0f);
    public Vector2 size = new Vector2(1f, 0.18f);
    public Color borderColor = new Color(0f, 0f, 0f, 0.9f);
    public Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    public Color highHealthColor = new Color(0.3f, 0.95f, 0.35f);
    public Color lowHealthColor = new Color(0.95f, 0.25f, 0.2f);

    private IDamageable target;
    private Image fillImage;
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

        GameObject border = new GameObject("Border", typeof(Image));
        border.transform.SetParent(transform, false);
        border.GetComponent<Image>().color = borderColor;
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-4f, -4f);
        borderRect.offsetMax = new Vector2(4f, 4f);

        GameObject background = new GameObject("Background", typeof(Image));
        background.transform.SetParent(transform, false);
        background.GetComponent<Image>().color = backgroundColor;
        StretchToFill(background.GetComponent<RectTransform>());

        GameObject fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(transform, false);
        fillImage = fill.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        StretchToFill(fill.GetComponent<RectTransform>());

        UpdateVisuals();
    }

    private void StretchToFill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (cam == null)
            cam = Camera.main;

        if (cam != null)
            transform.rotation = cam.transform.rotation;

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        float pct = target.MaxHealth > 0f ? target.CurrentHealth / target.MaxHealth : 0f;
        fillImage.fillAmount = pct;
        fillImage.color = Color.Lerp(lowHealthColor, highHealthColor, pct);
    }
}