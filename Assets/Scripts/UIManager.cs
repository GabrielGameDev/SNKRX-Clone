using UnityEngine;

/// <summary>
/// Gerenciador central de interface de usuário (UI).
/// Escuta os eventos do jogo (via GameEvents) sem acoplamento direto com as entidades de gameplay (como o PlayerExperience).
/// Coordena os componentes visuais do HUD e menus.
/// </summary>
[DisallowMultipleComponent]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Componentes de HUD")]
    [Tooltip("Referência para a barra de experiência no HUD.")]
    [SerializeField] private ExperienceBarUI experienceBar;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        // Inscreve-se nos eventos desacoplados
        GameEvents.OnExperienceChanged += HandleExperienceChanged;
        GameEvents.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        // Cancela a inscrição para evitar vazamentos de memória e erros de referência nula
        GameEvents.OnExperienceChanged -= HandleExperienceChanged;
        GameEvents.OnLevelUp -= HandleLevelUp;
    }

    private void HandleExperienceChanged(int currentXp, int xpToNextLevel, int currentLevel)
    {
        if (experienceBar != null)
        {
            experienceBar.SetExperience(currentXp, xpToNextLevel, currentLevel);
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        if (experienceBar != null)
        {
            experienceBar.OnLevelUp(newLevel);
        }
    }
}
