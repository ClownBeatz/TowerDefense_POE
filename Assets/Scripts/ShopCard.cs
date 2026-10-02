using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Shows one defender in the shop.</summary>
public class ShopCard : MonoBehaviour
{
    public Button button;
    public Image border;
    public Image accentStrip;
    public Image icon;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI costLabel;
    public TextMeshProUGUI hotkeyLabel;
    public CanvasGroup canvasGroup;

    public Color affordableCostColor = new Color(1f, 0.85f, 0.3f);
    public Color unaffordableCostColor = new Color(0.95f, 0.3f, 0.3f);
    public Color idleBorderColor = new Color(0f, 0f, 0f, 0.6f);

    private Color accentColor;

    public void Bind(Defender defender, int hotkey)
    {
        accentColor = defender.accentColor;
        accentStrip.color = accentColor;
        icon.sprite = defender.icon;
        icon.enabled = defender.icon != null;
        nameLabel.text = defender.displayName;
        costLabel.text = defender.cost.ToString();
        hotkeyLabel.text = hotkey.ToString();
    }

    public void Refresh(bool selected, bool affordable)
    {
        border.color = selected ? accentColor : idleBorderColor;
        costLabel.color = affordable ? affordableCostColor : unaffordableCostColor;
        canvasGroup.alpha = affordable ? 1f : 0.5f;
    }
}
