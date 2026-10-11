using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

/// <summary>Controls the title and game-over screens and records run results.</summary>
public class GameFlowController : MonoBehaviour
{
    private enum ScreenState { Title, Playing, GameOver }

    [Header("Scene UI")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private TextMeshProUGUI startPromptText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverTitleText;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField, Min(0f)] private float restartPromptDelay = 2.5f;

    private ScreenState state;
    private int highestWave;
    private int wavesCompleted;
    private int enemiesKilled;
    private int wanderersKilled;
    private int chasersKilled;
    private int kamikazesKilled;
    private int tanksKilled;
    private int levelsGained;
    private float damageReceived;
    private float runStartTime;
    private float restartPromptAvailableAt;
    private string gameOverSummary;
    private bool restartPromptShown;

    private void Awake()
    {
        state = ScreenState.Title;
        Time.timeScale = 0f;
        ShowTitle();
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDied += HandlePlayerDied;
        GameEvents.OnWaveStarted += HandleWaveStarted;
        GameEvents.OnWaveCompleted += HandleWaveCompleted;
        GameEvents.OnLevelUp += HandleLevelUp;
        GameEvents.OnPlayerDamageReceived += HandlePlayerDamage;
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        GameEvents.OnWaveStarted -= HandleWaveStarted;
        GameEvents.OnWaveCompleted -= HandleWaveCompleted;
        GameEvents.OnLevelUp -= HandleLevelUp;
        GameEvents.OnPlayerDamageReceived -= HandlePlayerDamage;
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void Update()
    {
        if (state == ScreenState.Playing) return;

        if (state == ScreenState.GameOver && !restartPromptShown)
        {
            if (Time.unscaledTime < restartPromptAvailableAt) return;
            detailsText.text = gameOverSummary + "\n\nPress any key to play again";
            restartPromptShown = true;
            return;
        }

        if (!AnyKeyPressedThisFrame()) return;

        if (state == ScreenState.Title)
        {
            state = ScreenState.Playing;
            MusicAudioController.SetGameplayState();
            runStartTime = Time.unscaledTime;
            titlePanel.SetActive(false);
            Time.timeScale = 1f;
        }
        else
        {
            ResetRunStats();
            state = ScreenState.Title;
            Time.timeScale = 0f;
            ShowTitle();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private static bool AnyKeyPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame)) return true;
        if (Gamepad.current != null)
        {
            foreach (var button in Gamepad.current.allControls)
                if (button is ButtonControl control && control.wasPressedThisFrame) return true;
        }
        return false;
    }

    private void HandlePlayerDied()
    {
        if (state != ScreenState.Playing) return;
        SfxPlayer.PlayGameOver();
        state = ScreenState.GameOver;
        MusicAudioController.SetPausedScreenState();
        Time.timeScale = 0f;
        ShowGameOver();
    }

    private void HandleWaveStarted(int wave) => highestWave = Mathf.Max(highestWave, wave);
    private void HandleWaveCompleted(int wave) => wavesCompleted++;
    private void HandleLevelUp(int level) => levelsGained++;
    private void HandlePlayerDamage(float amount) => damageReceived += Mathf.Max(0f, amount);

    private void ResetRunStats()
    {
        highestWave = 0;
        wavesCompleted = 0;
        enemiesKilled = 0;
        wanderersKilled = 0;
        chasersKilled = 0;
        kamikazesKilled = 0;
        tanksKilled = 0;
        levelsGained = 0;
        damageReceived = 0f;
        runStartTime = 0f;
    }

    private void HandleEnemyKilled(EnemyHealth enemy)
    {
        enemiesKilled++;
        if (enemy.GetComponent<EnemyTank>() != null) tanksKilled++;
        else if (enemy.GetComponent<EnemyKamikaze>() != null) kamikazesKilled++;
        else if (enemy.GetComponent<EnemyChaser>() != null) chasersKilled++;
        else wanderersKilled++;
    }

    private void ShowTitle()
    {
        MusicAudioController.SetMenuState();
        startPromptText.text = "Press any key to Start";
        gameOverPanel.SetActive(false);
        titlePanel.SetActive(true);
    }

    private void ShowGameOver()
    {
        MusicAudioController.SetPausedScreenState();
        float duration = Mathf.Max(0f, Time.unscaledTime - runStartTime);
        int score = enemiesKilled * 100 + wavesCompleted * 500 + levelsGained * 250;
        int level = FindAnyObjectByType<PlayerExperience>()?.CurrentLevel ?? levelsGained + 1;
        int totalXp = FindAnyObjectByType<PlayerExperience>()?.TotalExperienceAcquired ?? 0;
        gameOverTitleText.text = "Game Over";
        var summary = new StringBuilder();
        summary.AppendLine($"SCORE  {score:N0}                    TIME  {FormatTime(duration)}");
        summary.AppendLine($"Waves reached  {highestWave}  |  Completed  {wavesCompleted}");
        summary.AppendLine($"Enemies defeated  {enemiesKilled}  (Wanderer {wanderersKilled} · Chaser {chasersKilled} · Kamikaze {kamikazesKilled} · Tank {tanksKilled})");
        summary.AppendLine($"Level  {level}  |  Levels gained  {levelsGained}  |  XP collected  {totalXp}");
        summary.AppendLine($"Damage received  {damageReceived:0.#}");
        summary.AppendLine($"Upgrades unlocked  {FormatUpgradeCounts()}");
        summary.AppendLine();
        summary.Append("Score: 100 per enemy + 500 per completed wave + 250 per level gained");
        gameOverSummary = summary.ToString();
        detailsText.text = gameOverSummary;
        restartPromptAvailableAt = Time.unscaledTime + restartPromptDelay;
        restartPromptShown = false;
        titlePanel.SetActive(false);
        gameOverPanel.SetActive(true);
    }

    private static string FormatUpgradeCounts()
    {
        var manager = UpgradeManager.Instance;
        var counts = new StringBuilder();
        foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
        {
            if (counts.Length > 0) counts.Append(" · ");
            int count = manager != null ? manager.GetUpgradeState(type)?.Level ?? 0 : 0;
            counts.Append($"{type} {count}");
        }
        return counts.ToString();
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
}
