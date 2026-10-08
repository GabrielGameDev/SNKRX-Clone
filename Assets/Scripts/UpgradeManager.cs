using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia o sistema roguelike de upgrades da run.
/// Atribui aleatoriamente 6 cores aos 6 upgrades no início de cada run,
/// rastreia o estado de descoberta ('Fog of War') de cada cor,
/// e aplica os bônus e instancia os segmentos correspondentes na cobra.
/// </summary>
[DisallowMultipleComponent]
public class UpgradeManager : MonoBehaviour
{
    private static UpgradeManager _instance;
    public static UpgradeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<UpgradeManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("UpgradeManager");
                    _instance = go.AddComponent<UpgradeManager>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Prefabs dos Segmentos")]
    [Tooltip("Prefab do segmento atirador (Shooter Snake).")]
    [SerializeField] private SnakeSegment shooterSegmentPrefab;

    [Tooltip("Prefab do segmento explosivo (Explosion Snake).")]
    [SerializeField] private SnakeSegment explosionSegmentPrefab;

    [Tooltip("Prefab do segmento normal do corpo (Snake Segment).")]
    [SerializeField] private SnakeSegment defaultSegmentPrefab;

    [Header("Valores Base de Stats")]
    [SerializeField] private float baseMagnetRadius = 2.0f;
    [SerializeField] private float baseMagnetPullSpeed = 7.0f;

    // Modificadores de gameplay acumulados na run
    public float GlobalDamageMultiplier { get; private set; } = 1.0f;
    public float RangeMultiplier { get; private set; } = 1.0f;
    public float BonusMaxHealth { get; private set; } = 0.0f;
    public float MagnetRadius { get; private set; } = 2.0f;
    public float MagnetPullSpeed { get; private set; } = 7.0f;

    public static event Action<RunUpgradeState> OnUpgradeApplied;
    public static event Action OnRunReset;

    private readonly Dictionary<UpgradeType, RunUpgradeState> upgrades = new Dictionary<UpgradeType, RunUpgradeState>();

    // Definição das 6 cores solicitadas
    private readonly List<UpgradeColorInfo> palette = new List<UpgradeColorInfo>()
    {
        new UpgradeColorInfo("Vermelho", new Color(1f, 0.27f, 0.27f, 1f), "#FF4545"),
        new UpgradeColorInfo("Verde",    new Color(0.18f, 0.8f, 0.44f, 1f), "#2ECC71"),
        new UpgradeColorInfo("Azul",     new Color(0.2f, 0.6f, 1f, 1f),    "#3498DB"),
        new UpgradeColorInfo("Amarelo",  new Color(0.95f, 0.77f, 0.06f, 1f), "#F1C40F"),
        new UpgradeColorInfo("Roxo",     new Color(0.61f, 0.35f, 0.71f, 1f), "#9B59B6"),
        new UpgradeColorInfo("Branco",   new Color(0.94f, 0.94f, 0.95f, 1f), "#ECF0F1")
    };

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        LoadPrefabsIfNeeded();
        InitializeUpgradesData();
        StartNewRun();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void LoadPrefabsIfNeeded()
    {
#if UNITY_EDITOR
        if (shooterSegmentPrefab == null)
        {
            shooterSegmentPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<SnakeSegment>("Assets/Prefabs/Shooter Snake.prefab");
        }
        if (explosionSegmentPrefab == null)
        {
            explosionSegmentPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<SnakeSegment>("Assets/Prefabs/Explosion Snake.prefab");
        }
        if (defaultSegmentPrefab == null)
        {
            defaultSegmentPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<SnakeSegment>("Assets/Prefabs/Snake Segment.prefab");
        }
#endif
    }

    /// <summary>
    /// Cadastra os 6 upgrades com seus nomes e descrições quando descobertos.
    /// </summary>
    private void InitializeUpgradesData()
    {
        upgrades.Clear();

        upgrades[UpgradeType.Shooter] = new RunUpgradeState(
            UpgradeType.Shooter,
            "Tiro Automático",
            "Cria um novo segmento Shooter Snake que dispara continuamente contra o inimigo mais próximo."
        );

        upgrades[UpgradeType.Damage] = new RunUpgradeState(
            UpgradeType.Damage,
            "Dano Global & Explosão",
            "Aumenta o dano de todas as fontes em +25% e cria um segmento Explosion Snake que explode em área periodicamente."
        );

        upgrades[UpgradeType.Range] = new RunUpgradeState(
            UpgradeType.Range,
            "Alcance Ampliado",
            "Aumenta o alcance de ataque dos atiradores e o raio das explosões em +20% e adiciona uma bolinha ao corpo."
        );

        upgrades[UpgradeType.Health] = new RunUpgradeState(
            UpgradeType.Health,
            "Vitalidade dos Segmentos",
            "Aumenta a vida máxima dos segmentos em +1.5 HP, cura a cobra e adiciona uma bolinha ao corpo."
        );

        upgrades[UpgradeType.Speed] = new RunUpgradeState(
            UpgradeType.Speed,
            "Velocidade da Cobra",
            "Aumenta a velocidade de movimento da cobra em +15% e adiciona uma bolinha ao corpo."
        );

        upgrades[UpgradeType.Magnet] = new RunUpgradeState(
            UpgradeType.Magnet,
            "Ímã de XP",
            "Aumenta o raio de atração para coletar orbes de experiência em +3.0 metros e adiciona uma bolinha ao corpo."
        );
    }

    /// <summary>
    /// Inicia ou reinicia uma run:
    /// Embaralha aleatoriamente as 6 cores entre os 6 upgrades e oculta todas as identidades (Fog of War).
    /// </summary>
    public void StartNewRun()
    {
        GlobalDamageMultiplier = 1.0f;
        RangeMultiplier = 1.0f;
        BonusMaxHealth = 0.0f;
        MagnetRadius = baseMagnetRadius;
        MagnetPullSpeed = baseMagnetPullSpeed;

        // Embaralha uma cópia da paleta de cores (Fisher-Yates)
        List<UpgradeColorInfo> shuffledColors = new List<UpgradeColorInfo>(palette);
        for (int i = 0; i < shuffledColors.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(i, shuffledColors.Count);
            UpgradeColorInfo temp = shuffledColors[i];
            shuffledColors[i] = shuffledColors[rnd];
            shuffledColors[rnd] = temp;
        }

        // Atribui uma cor única a cada um dos 6 upgrades e reseta o estado de descoberta
        int colorIdx = 0;
        foreach (var kvp in upgrades)
        {
            RunUpgradeState state = kvp.Value;
            state.AssignedColor = shuffledColors[colorIdx++];
            state.IsDiscovered = false;
            state.Level = 0;
        }

        OnRunReset?.Invoke();
    }

    /// <summary>
    /// Retorna o estado atual de um upgrade específico.
    /// </summary>
    public RunUpgradeState GetUpgradeState(UpgradeType type)
    {
        return upgrades.TryGetValue(type, out var state) ? state : null;
    }

    /// <summary>
    /// Sorteia 'count' upgrades distintos entre as opções disponíveis (padrão roguelike: 3).
    /// </summary>
    public List<RunUpgradeState> GetRandomChoices(int count = 3)
    {
        List<RunUpgradeState> all = new List<RunUpgradeState>(upgrades.Values);
        // Embaralha
        for (int i = 0; i < all.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(i, all.Count);
            var temp = all[i];
            all[i] = all[rnd];
            all[rnd] = temp;
        }

        int take = Mathf.Clamp(count, 1, all.Count);
        return all.GetRange(0, take);
    }

    /// <summary>
    /// Aplica o upgrade escolhido pelo jogador:
    /// 1. Revela a cor para o resto da run (IsDiscovered = true).
    /// 2. Aplica o efeito mecânico do upgrade.
    /// 3. Adiciona um segmento com a cor da run correspondente.
    /// </summary>
    public void SelectUpgrade(UpgradeType type)
    {
        if (!upgrades.TryGetValue(type, out RunUpgradeState state))
        {
            Debug.LogError($"[UpgradeManager] Tipo de upgrade inválido: {type}");
            return;
        }

        // Revela para o resto da run
        state.IsDiscovered = true;
        state.Level++;

        Color segmentColor = state.AssignedColor.color;
        SnakeMovement snake = SnakeMovement.Instance;

        switch (type)
        {
            case UpgradeType.Shooter:
                if (snake != null)
                {
                    snake.AddSegment(shooterSegmentPrefab, segmentColor);
                }
                break;

            case UpgradeType.Damage:
                GlobalDamageMultiplier += 0.25f;
                if (snake != null)
                {
                    snake.AddSegment(explosionSegmentPrefab, segmentColor);
                }
                break;

            case UpgradeType.Range:
                RangeMultiplier += 0.20f;
                if (snake != null)
                {
                    snake.AddSegment(defaultSegmentPrefab, segmentColor);
                }
                break;

            case UpgradeType.Health:
                BonusMaxHealth += 1.5f;
                // Aumenta a vida de todos os segmentos já existentes
                if (snake != null)
                {
                    var headHealth = snake.GetComponent<SegmentHealth>();
                    if (headHealth != null)
                    {
                        headHealth.IncreaseMaxHealth(1.5f, true);
                    }

                    for (int i = 0; i < snake.Segments.Count; i++)
                    {
                        var segHealth = snake.Segments[i].GetComponent<SegmentHealth>();
                        if (segHealth != null)
                        {
                            segHealth.IncreaseMaxHealth(1.5f, true);
                        }
                    }

                    snake.AddSegment(defaultSegmentPrefab, segmentColor);
                }
                break;

            case UpgradeType.Speed:
                if (snake != null)
                {
                    snake.MoveSpeed *= 1.15f;
                    snake.AddSegment(defaultSegmentPrefab, segmentColor);
                }
                break;

            case UpgradeType.Magnet:
                MagnetRadius += 3.0f;
                MagnetPullSpeed += 2.0f;
                if (snake != null)
                {
                    snake.AddSegment(defaultSegmentPrefab, segmentColor);
                }
                break;
        }

        OnUpgradeApplied?.Invoke(state);
    }
}
