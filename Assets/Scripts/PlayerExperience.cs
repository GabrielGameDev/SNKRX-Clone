using System;
using UnityEngine;

/// <summary>
/// Armazena o XP e nível do jogador, gerenciando progressão e subida de nível.
/// Notifica ouvintes de forma desacoplada através do GameEvents.
/// </summary>
[DisallowMultipleComponent]
public class PlayerExperience : MonoBehaviour
{
    [Header("Configurações Iniciais")]
    [Tooltip("XP inicial do jogador dentro do nível 1.")]
    [Min(0)]
    [SerializeField] private int startingExperience = 0;

    [Tooltip("Nível inicial do jogador.")]
    [Min(1)]
    [SerializeField] private int startingLevel = 1;

    [Header("Progressão de Nível")]
    [Tooltip("Quantidade de XP necessária para avançar do nível 1 para o nível 2.")]
    [Min(1)]
    [SerializeField] private int baseExperienceToLevel = 5;

    [Tooltip("Aumento incremental no XP necessário para cada novo nível.")]
    [Min(0)]
    [SerializeField] private int experienceIncreasePerLevel = 3;

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentExperience { get; private set; }
    public int TotalExperienceAcquired { get; private set; }

    /// <summary>XP necessário para atingir o próximo nível no nível atual.</summary>
    public int ExperienceToNextLevel => GetRequiredExperience(CurrentLevel);

    /// <summary>Dispara com o total acumulado de XP (mantido para compatibilidade).</summary>
    public event Action<int> OnExperienceChanged;

    private void Awake()
    {
        CurrentLevel = Mathf.Max(1, startingLevel);
        CurrentExperience = Mathf.Max(0, startingExperience);
        TotalExperienceAcquired = CurrentExperience;
    }

    private void Start()
    {
        // Publica o estado inicial para que a UI e outros sistemas se inicializem com os valores corretos.
        GameEvents.TriggerExperienceChanged(CurrentExperience, ExperienceToNextLevel, CurrentLevel);
        OnExperienceChanged?.Invoke(CurrentExperience);
    }

    /// <summary>Calcula a meta de XP necessária para concluir um determinado nível.</summary>
    public int GetRequiredExperience(int level)
    {
        int levelOffset = Mathf.Max(0, level - 1);
        return Mathf.Max(1, baseExperienceToLevel + levelOffset * experienceIncreasePerLevel);
    }

    /// <summary>Adiciona XP ao total do nível atual e processa possíveis subidas de nível.</summary>
    public void AddExperience(int amount)
    {
        if (amount <= 0) return;

        CurrentExperience += amount;
        TotalExperienceAcquired += amount;

        // Processa subidas de nível progressivas
        while (CurrentExperience >= ExperienceToNextLevel)
        {
            CurrentExperience -= ExperienceToNextLevel;
            CurrentLevel++;
            GameEvents.TriggerLevelUp(CurrentLevel);
        }

        // Notifica através do canal desacoplado
        GameEvents.TriggerExperienceChanged(CurrentExperience, ExperienceToNextLevel, CurrentLevel);
        OnExperienceChanged?.Invoke(CurrentExperience);
    }
}
