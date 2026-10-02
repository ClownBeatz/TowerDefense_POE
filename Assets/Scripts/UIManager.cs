using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Updates the HUD and menus.</summary>
public class UIManager : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public DefenderPlacer defenderPlacer;
    public EnemySpawner enemySpawner;

    [Header("Tower")]
    public HealthBarFill towerHealthBar;
    public TextMeshProUGUI towerHealthText;

    [Header("Economy")]
    public TextMeshProUGUI resourcesText;

    [Header("Shop")]
    public ShopCard[] shopCards;

    [Header("Waves")]
    public TextMeshProUGUI waveLabel;
    public Image waveProgressFill;
    public Image waveStatusIcon;
    public TextMeshProUGUI waveStatusText;
    public Sprite countdownSprite;
    public Sprite enemySprite;
    public Color countdownColor = new Color(0.35f, 0.7f, 1f);
    public Color enemiesColor = new Color(0.95f, 0.3f, 0.25f);
    public TextMeshProUGUI bannerLabel;
    public float bannerDuration = 2.5f;

    [Header("Wave Director Panel")]
    public GameObject directorPanel;
    public Image difficultyFill;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI directorLabel;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverStatsLabel;
    public Button playAgainButton;

    [Header("Pause")]
    public GameObject pausedOverlay;
    public Button resumeButton;
    public Button pausedRestartButton;

    [Header("Buttons")]
    public Button pauseButton;
    public Button restartButton;

    private Coroutine bannerRoutine;

    private void OnEnable()
    {
        Tower.OnTowerHealthChanged += UpdateTowerHealth;
        GameManager.OnResourcesChanged += UpdateResources;
        GameManager.OnGameOver += ShowGameOver;
        GameManager.OnGameRestarted += HandleRestart;
        GameManager.OnPauseChanged += UpdatePaused;
        EnemySpawner.OnWaveStarted += HandleWaveStarted;
        EnemySpawner.OnWaveCompleted += HandleWaveCompleted;

        if (defenderPlacer != null)
            defenderPlacer.OnSelectionChanged += HandleSelectionChanged;
    }

    private void OnDisable()
    {
        Tower.OnTowerHealthChanged -= UpdateTowerHealth;
        GameManager.OnResourcesChanged -= UpdateResources;
        GameManager.OnGameOver -= ShowGameOver;
        GameManager.OnGameRestarted -= HandleRestart;
        GameManager.OnPauseChanged -= UpdatePaused;
        EnemySpawner.OnWaveStarted -= HandleWaveStarted;
        EnemySpawner.OnWaveCompleted -= HandleWaveCompleted;

        if (defenderPlacer != null)
            defenderPlacer.OnSelectionChanged -= HandleSelectionChanged;
    }

    private void Start()
    {
        gameOverPanel.SetActive(false);

        pauseButton.onClick.AddListener(gameManager.TogglePause);
        restartButton.onClick.AddListener(gameManager.StartNewGame);
        AddListener(resumeButton, gameManager.TogglePause);
        AddListener(pausedRestartButton, gameManager.StartNewGame);
        AddListener(playAgainButton, gameManager.StartNewGame);

        SetupShop();
        RefreshDirectorPanel();
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private void Update()
    {
        UpdateWaveProgress();

        if (Input.GetKeyDown(KeyCode.Tab) && directorPanel != null)
            directorPanel.SetActive(!directorPanel.activeSelf);
    }

    //Shop

    private void SetupShop()
    {
        for (int i = 0; i < shopCards.Length; i++)
        {
            bool hasDefender = i < defenderPlacer.defenderPrefabs.Count;
            shopCards[i].gameObject.SetActive(hasDefender);
            if (!hasDefender) continue;

            int index = i;
            shopCards[i].Bind(defenderPlacer.defenderPrefabs[i], i + 1);
            shopCards[i].button.onClick.AddListener(() => defenderPlacer.SelectDefender(index));
        }

        RefreshShop();
    }

    private void RefreshShop()
    {
        if (shopCards == null || defenderPlacer == null) return;

        int resources = gameManager.CurrentResources;

        for (int i = 0; i < shopCards.Length && i < defenderPlacer.defenderPrefabs.Count; i++)
        {
            bool affordable = resources >= defenderPlacer.defenderPrefabs[i].cost;
            shopCards[i].Refresh(i == defenderPlacer.SelectedIndex, affordable);
        }
    }

    private void HandleSelectionChanged(Defender selected)
    {
        RefreshShop();
    }

    //Waves

    // Between waves the bar fills up towards the next wave; during a wave it
    // drains as enemies are killed.
    private void UpdateWaveProgress()
    {
        if (enemySpawner == null || waveProgressFill == null) return;

        if (enemySpawner.WaveInProgress && enemySpawner.CurrentPlan != null)
        {
            int total = Mathf.Max(enemySpawner.CurrentPlan.Spawns.Count, 1);
            int remaining = enemySpawner.EnemiesRemaining;
            waveProgressFill.fillAmount = (float)remaining / total;
            waveProgressFill.color = enemiesColor;
            waveStatusIcon.sprite = enemySprite;
            waveStatusText.text = remaining.ToString();
        }
        else
        {
            float delay = Mathf.Max(enemySpawner.CurrentIntermission, 0.01f);
            waveProgressFill.fillAmount = 1f - enemySpawner.Countdown / delay;
            waveProgressFill.color = countdownColor;
            waveStatusIcon.sprite = countdownSprite;
            waveStatusText.text = Mathf.CeilToInt(enemySpawner.Countdown) + "s";
        }
    }

    private void HandleWaveStarted(WavePlan plan)
    {
        waveLabel.text = "WAVE " + plan.Wave;
        ShowBanner(plan.IsSurge ? $"WAVE {plan.Wave}: SURGE!" : $"WAVE {plan.Wave}");
        RefreshDirectorPanel();
    }

    private void HandleWaveCompleted(WaveReport report)
    {
        ShowBanner($"Wave {report.Wave} cleared");
        RefreshDirectorPanel();
    }

    private void RefreshDirectorPanel()
    {
        if (enemySpawner == null) return;

        float difficulty = enemySpawner.Difficulty;
        float meter = Mathf.InverseLerp(enemySpawner.minDifficulty, enemySpawner.maxDifficulty, difficulty);
        difficultyFill.fillAmount = meter;
        difficultyFill.color = Color.Lerp(new Color(0.3f, 0.85f, 0.4f), new Color(0.95f, 0.3f, 0.25f), meter);
        difficultyText.text = $"{difficulty:0.00}x  {enemySpawner.SkillLabel}";

        StringBuilder text = new StringBuilder();

        WavePlan plan = enemySpawner.CurrentPlan;
        if (plan != null)
        {
            text.AppendLine($"<b>Wave {plan.Wave} plan</b>  {plan.Budget} pts, HP x{plan.HealthMultiplier:0.00}");
            text.AppendLine($"<color=#FFD966>{plan.MixSummary()}</color>");
            foreach (string reason in plan.Reasons)
                text.AppendLine("- " + reason);
        }

        WaveReport report = enemySpawner.LastReport;
        if (report != null)
        {
            text.AppendLine();
            text.AppendLine($"<b>Wave {report.Wave} result</b>  tower -{report.TowerDamagePercent:P0}, " +
                            $"{report.ClearTime:0}s, {report.DefendersLost} defenders lost");
            foreach (string adjustment in enemySpawner.LastAdjustments)
                text.AppendLine("- " + adjustment);
        }

        directorLabel.text = text.Length > 0 ? text.ToString() : "Waiting for the first wave...";
    }

    private void ShowBanner(string message)
    {
        if (bannerLabel == null) return;

        if (bannerRoutine != null)
            StopCoroutine(bannerRoutine);

        bannerRoutine = StartCoroutine(BannerRoutine(message));
    }

    private IEnumerator BannerRoutine(string message)
    {
        bannerLabel.text = message;

        for (float t = 0f; t < bannerDuration; t += Time.deltaTime)
        {
            // Fully visible for the first half, then fade out.
            bannerLabel.alpha = Mathf.Clamp01(2f - 2f * t / bannerDuration);
            yield return null;
        }

        bannerLabel.alpha = 0f;
        bannerRoutine = null;
    }

    //Tower, economy, game state

    private void UpdateTowerHealth(float current, float max)
    {
        // A full-health report means a fresh tower, so snap the bar instead of animating.
        towerHealthBar.SetValue(max > 0f ? current / max : 0f, current >= max);
        towerHealthText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
    }

    private void UpdateResources(int amount)
    {
        resourcesText.text = amount.ToString();
        RefreshShop();
    }

    private void UpdatePaused(bool paused)
    {
        pausedOverlay.SetActive(paused);
    }

    private void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
        gameOverStatsLabel.text = $"You held out until wave <b>{enemySpawner.CurrentWave}</b>";
    }

    private void HandleRestart()
    {
        gameOverPanel.SetActive(false);
        waveLabel.text = "GET READY";

        if (bannerLabel != null)
            bannerLabel.alpha = 0f;

        RefreshDirectorPanel();
    }
}
