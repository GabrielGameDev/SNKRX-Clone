using System;

/// <summary>
/// Canal global de eventos para comunicação desacoplada entre sistemas do jogo.
/// O PlayerExperience e o UIManager não se comunicam diretamente; ambos apenas interagem com estes eventos.
/// </summary>
public static class GameEvents
{
    /// <summary>
    /// Disparado quando o XP do jogador muda.
    /// Parâmetros: (int currentXp, int xpToNextLevel, int currentLevel)
    /// </summary>
    public static event Action<int, int, int> OnExperienceChanged;

    /// <summary>
    /// Disparado quando o jogador sobe de nível.
    /// Parâmetro: (int newLevel)
    /// </summary>
    public static event Action<int> OnLevelUp;

    /// <summary>
    /// Disparado quando a cabeça da cobra é derrotada.
    /// </summary>
    public static event Action OnPlayerDied;
    // Nova API de ondas
    /// <summary>
    /// Disparado quando uma nova onda começa. Parâmetro: número da onda (1‑based).
    /// </summary>
    public static event Action<int> OnWaveStarted;
    /// <summary>
    /// Disparado quando todos os inimigos da onda atual morrem.
    /// </summary>
    public static event Action<int> OnWaveCompleted;
    public static event Action<float> OnPlayerDamageReceived;
    public static event Action<EnemyHealth> OnEnemyKilled;
    public static event Action<string, float> OnSegmentDamageDealt;

    /// <summary>
    /// Helper para disparar o evento de início de onda.
    /// </summary>
    public static void TriggerWaveStarted(int wave)
    {
        OnWaveStarted?.Invoke(wave);
    }

    /// <summary>
    /// Helper para disparar o evento de final de onda.
    /// </summary>
    public static void TriggerWaveCompleted(int wave)
    {
        OnWaveCompleted?.Invoke(wave);
    }

    public static void TriggerExperienceChanged(int currentXp, int xpToNextLevel, int currentLevel)
    {
        OnExperienceChanged?.Invoke(currentXp, xpToNextLevel, currentLevel);
    }

    public static void TriggerLevelUp(int newLevel)
    {
        OnLevelUp?.Invoke(newLevel);
    }

    public static void TriggerPlayerDied()
    {
        OnPlayerDied?.Invoke();
    }

    public static void TriggerPlayerDamageReceived(float amount)
    {
        OnPlayerDamageReceived?.Invoke(amount);
    }

    public static void TriggerEnemyKilled(EnemyHealth enemy)
    {
        OnEnemyKilled?.Invoke(enemy);
    }

    public static void TriggerSegmentDamageDealt(string sourceType, float amount)
    {
        OnSegmentDamageDealt?.Invoke(sourceType, amount);
    }
}
