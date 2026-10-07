using System;
using UnityEngine;

/// <summary>
/// Armazena o XP acumulado pelo jogador e notifica os ouvintes quando ele muda.
/// Deve ser anexado ao objeto do jogador.
/// </summary>
[DisallowMultipleComponent]
public class PlayerExperience : MonoBehaviour
{
    [Tooltip("XP inicial do jogador.")]
    [Min(0)]
    [SerializeField] private int startingExperience;

    public int CurrentExperience { get; private set; }

    /// <summary>Dispara com o total atualizado de XP.</summary>
    public event Action<int> OnExperienceChanged;

    private void Awake()
    {
        CurrentExperience = startingExperience;
    }

    /// <summary>Adiciona XP ao total. Valores negativos ou zero são ignorados.</summary>
    public void AddExperience(int amount)
    {
        if (amount <= 0) return;

        CurrentExperience += amount;
        OnExperienceChanged?.Invoke(CurrentExperience);
    }
}
