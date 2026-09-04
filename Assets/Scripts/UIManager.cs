// UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public DefenderPlacer defenderPlacer;

    [Header("Tower")]
    public Slider towerHealthSlider;
    public TextMeshProUGUI towerHealthLabel;

    [Header("Economy")]
    public TextMeshProUGUI resourcesLabel;
    public TextMeshProUGUI defenderCostLabel;
    public Color affordableColor = Color.white;
    public Color unaffordableColor = new Color(0.95f, 0.35f, 0.35f);

    [Header("Game Over")]
    public GameObject gameOverPanel;

    [Header("Buttons")]
    public Button pauseButton;
    public Button restartButton;

    private void OnEnable()
    {
        Tower.OnTowerHealthChanged += UpdateTowerHealth;
        GameManager.OnResourcesChanged += UpdateResources;
        GameManager.OnGameOver += ShowGameOver;
        GameManager.OnGameRestarted += HideGameOver;
    }

    private void OnDisable()
    {
        Tower.OnTowerHealthChanged -= UpdateTowerHealth;
        GameManager.OnResourcesChanged -= UpdateResources;
        GameManager.OnGameOver -= ShowGameOver;
        GameManager.OnGameRestarted -= HideGameOver;
    }

    private void Start()
    {
        if (defenderCostLabel != null && defenderPlacer != null)
            defenderCostLabel.text = "Cost: " + defenderPlacer.defenderCost;

        gameOverPanel.SetActive(false);

        pauseButton.onClick.AddListener(() => gameManager.TogglePause());
        restartButton.onClick.AddListener(() => gameManager.StartNewGame());
    }

    private void UpdateTowerHealth(float current, float max)
    {
        towerHealthSlider.maxValue = max;
        towerHealthSlider.value = current;
        towerHealthLabel.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
    }

    private void UpdateResources(int amount)
    {
        resourcesLabel.text = "Resources: " + amount;

        if (defenderCostLabel != null && defenderPlacer != null)
            defenderCostLabel.color = amount >= defenderPlacer.defenderCost ? affordableColor : unaffordableColor;
    }

    private void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
    }

    private void HideGameOver()
    {
        gameOverPanel.SetActive(false);
    }
}