using System.Text;
using System;
using System.Collections.Generic;
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
    private float shooterDamageDealt;
    private float bomberDamageDealt;
    private int currentWave;
    private string runId;
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
        GameEvents.OnSegmentDamageDealt += HandleSegmentDamageDealt;
        UpgradeManager.OnUpgradeApplied += HandleUpgradeApplied;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        GameEvents.OnWaveStarted -= HandleWaveStarted;
        GameEvents.OnWaveCompleted -= HandleWaveCompleted;
        GameEvents.OnLevelUp -= HandleLevelUp;
        GameEvents.OnPlayerDamageReceived -= HandlePlayerDamage;
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        GameEvents.OnSegmentDamageDealt -= HandleSegmentDamageDealt;
        UpgradeManager.OnUpgradeApplied -= HandleUpgradeApplied;
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
            runId = Guid.NewGuid().ToString("N");
            GameAnalyticsReporter.SetRunId(runId);
            RecordRunEvent("run_started", new Dictionary<string, object>());
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

    private void HandleWaveStarted(int wave)
    {
        currentWave = wave;
        highestWave = Mathf.Max(highestWave, wave);
        if (state == ScreenState.Playing)
            RecordRunEvent("wave_started", new Dictionary<string, object> { { "wave", wave } });
    }

    private void HandleWaveCompleted(int wave)
    {
        wavesCompleted++;
        if (state == ScreenState.Playing)
            RecordRunEvent("wave_completed", new Dictionary<string, object> { { "wave", wave } });
    }

    private void HandleLevelUp(int level) => levelsGained++;
    private void HandlePlayerDamage(float amount) => damageReceived += Mathf.Max(0f, amount);

    private void HandleUpgradeApplied(RunUpgradeState upgrade)
    {
        if (state != ScreenState.Playing || upgrade == null) return;
        RecordRunEvent("upgrade_selected", new Dictionary<string, object>
        {
            { "upgrade_type", upgrade.Type.ToString().ToLowerInvariant() },
            { "upgrade_levelNew", upgrade.Level },
            { "wave", currentWave }
        });
    }

    private void HandleSegmentDamageDealt(string sourceType, float amount)
    {
        if (state != ScreenState.Playing || amount <= 0f) return;
        if (sourceType == "shooter") shooterDamageDealt += amount;
        else if (sourceType == "bomber") bomberDamageDealt += amount;
    }

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
        shooterDamageDealt = 0f;
        bomberDamageDealt = 0f;
        currentWave = 0;
        runStartTime = 0f;
        runId = null;
        GameAnalyticsReporter.SetRunId(null);
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
        RecordRunEvent("run_ended", new Dictionary<string, object>
        {
            { "duration_seconds", duration },
            { "score", score },
            { "highest_wave", highestWave },
            { "waves_completed", wavesCompleted },
            { "player_level", level },
            { "levels_gained", levelsGained },
            { "xp_collected", totalXp },
            { "enemies_killed", enemiesKilled },
            { "wanderers_killed", wanderersKilled },
            { "chasers_killed", chasersKilled },
            { "kamikazes_killed", kamikazesKilled },
            { "tanks_killed", tanksKilled },
            { "damage_received", damageReceived },
            { "shooter_damage", shooterDamageDealt },
            { "bomber_damage", bomberDamageDealt },
            { "upgrade_shooter", GetUpgradeLevel(UpgradeType.Shooter) },
            { "upgrade_damage", GetUpgradeLevel(UpgradeType.Damage) },
            { "upgrade_range", GetUpgradeLevel(UpgradeType.Range) },
            { "upgrade_health", GetUpgradeLevel(UpgradeType.Health) },
            { "upgrade_speed", GetUpgradeLevel(UpgradeType.Speed) },
            { "upgrade_magnet", GetUpgradeLevel(UpgradeType.Magnet) }
        });
        GameAnalyticsReporter.Flush();
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

    private int GetUpgradeLevel(UpgradeType type) => UpgradeManager.Instance?.GetUpgradeState(type)?.Level ?? 0;

    private void RecordRunEvent(string eventName, Dictionary<string, object> parameters)
    {
        parameters["run_id"] = runId ?? string.Empty;
        GameAnalyticsReporter.Record(eventName, parameters);
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
