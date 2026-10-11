using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeBalanceData", menuName = "SNKRX/Upgrade Balance Data")]
public class UpgradeBalanceData : ScriptableObject
{
    [Serializable]
    public class UpgradeDefinition
    {
        public UpgradeType type;
        public string displayName;
        [TextArea(2, 5)] public string description;
        [Min(0f)] public float amount;
    }

    [Header("Stats iniciais")]
    public float globalDamageMultiplier = 1f;
    public float rangeMultiplier = 1f;
    public float baseSegmentHealth = 3f;
    public float baseMagnetRadius = 2f;
    public float baseMagnetPullSpeed = 7f;

    [Header("Upgrades (amount é o bônus por nível)")]
    public List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>
    {
        new UpgradeDefinition { type = UpgradeType.Shooter, displayName = "Automatic Fire", description = "Adds a Shooter Snake segment that continuously fires at the nearest enemy." },
        new UpgradeDefinition { type = UpgradeType.Damage, displayName = "Global Damage & Explosion", description = "Increases damage from all sources by +25% and adds an Explosion Snake segment that periodically deals area damage.", amount = 0.25f },
        new UpgradeDefinition { type = UpgradeType.Range, displayName = "Extended Range", description = "Increases shooters' attack range and explosion radius by +20%, and adds a segment to the body.", amount = 0.20f },
        new UpgradeDefinition { type = UpgradeType.Health, displayName = "Segment Vitality", description = "Increases segment maximum health by +1.5 HP, heals the snake, and adds a segment to the body.", amount = 1.5f },
        new UpgradeDefinition { type = UpgradeType.Speed, displayName = "Snake Speed", description = "Increases the snake's movement speed by +15% and adds a segment to the body.", amount = 0.15f },
        new UpgradeDefinition { type = UpgradeType.Magnet, displayName = "XP Magnet", description = "Increases the pickup radius for experience orbs by +3.0 and adds a segment to the body.", amount = 3f }
    };

    public UpgradeDefinition Get(UpgradeType type) => upgrades.Find(item => item.type == type);
}
