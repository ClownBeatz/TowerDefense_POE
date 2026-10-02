using UnityEngine;
using UnityEngine.UI;

/// <summary>Animates a health bar fill.</summary>
public class HealthBarFill : MonoBehaviour
{
    public Image fill;
    public Image chip;
    public Color highColor = new Color(0.3f, 0.95f, 0.35f);
    public Color lowColor = new Color(0.95f, 0.25f, 0.2f);
    public Color flashColor = Color.white;
    public float chipDelay = 0.35f;
    public float chipDrainSpeed = 0.8f;
    public float flashDuration = 0.12f;

    private float target = 1f;
    private float chipTimer;
    private float flashTimer;

    public void SetValue(float percent, bool instant = false)
    {
        percent = Mathf.Clamp01(percent);

        if (percent < target - 0.0001f && !instant)
        {
            chipTimer = chipDelay;
            flashTimer = flashDuration;
        }

        target = percent;
        fill.fillAmount = percent;

        if (chip != null && (instant || chip.fillAmount < percent))
            chip.fillAmount = percent;

        UpdateColour();
    }

    private void Update()
    {
        if (chip != null)
        {
            if (chipTimer > 0f)
                chipTimer -= Time.deltaTime;
            else
                chip.fillAmount = Mathf.MoveTowards(chip.fillAmount, target, chipDrainSpeed * Time.deltaTime);
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            UpdateColour();
        }
    }

    private void UpdateColour()
    {
        fill.color = flashTimer > 0f ? flashColor : Color.Lerp(lowColor, highColor, target);
    }
}
