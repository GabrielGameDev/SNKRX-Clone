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

    public static void TriggerExperienceChanged(int currentXp, int xpToNextLevel, int currentLevel)
    {
        OnExperienceChanged?.Invoke(currentXp, xpToNextLevel, currentLevel);
    }

    public static void TriggerLevelUp(int newLevel)
    {
        OnLevelUp?.Invoke(newLevel);
    }
}
