using System;
using UnityEngine;

/// <summary>
/// Tipos de upgrades disponíveis no jogo roguelike.
/// </summary>
public enum UpgradeType
{
    Shooter, // Tiro: cria um segmento Shooter Snake
    Damage,  // Dano: aumenta o dano global e cria um Explosion Segment
    Range,   // Alcance: aumenta o raio de alcance dos segment shooter e segment bomber
    Health,  // Vida: aumenta a vida máxima dos segmentos
    Speed,   // Velocidade: aumenta a velocidade da cobrinha
    Magnet   // Ímã: aumenta o raio de atração pra coletar os XpPickups
}

/// <summary>
/// Representa uma das 6 cores da run com nome e código hexadecimal.
/// </summary>
[Serializable]
public class UpgradeColorInfo
{
    public string colorName;
    public Color color;
    public string hexCode;

    public UpgradeColorInfo(string colorName, Color color, string hexCode)
    {
        this.colorName = colorName;
        this.color = color;
        this.hexCode = hexCode;
    }
}

/// <summary>
/// Estado de um upgrade durante a run ativa.
/// </summary>
public class RunUpgradeState
{
    public UpgradeType Type { get; }
    public string DisplayName { get; }
    public string DiscoveredDescription { get; }
    public UpgradeColorInfo AssignedColor { get; set; }
    public bool IsDiscovered { get; set; }
    public int Level { get; set; }

    public RunUpgradeState(UpgradeType type, string displayName, string discoveredDescription)
    {
        Type = type;
        DisplayName = displayName;
        DiscoveredDescription = discoveredDescription;
        IsDiscovered = false;
        Level = 0;
    }
}
