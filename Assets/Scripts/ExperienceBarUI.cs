using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla a visualização da barra de experiência no topo da tela (estilo Vampire Survivors).
/// Atualiza o preenchimento suave da barra e os textos de nível e progresso.
/// </summary>
public class ExperienceBarUI : MonoBehaviour
{
    [Header("Elementos Visuais")]
    [Tooltip("Imagem da barra. O preenchimento é controlado pela escala X do RectTransform; o sprite é opcional.")]
    [SerializeField] private Image fillImage;

    [Tooltip("Texto opcional para exibir o nível atual (ex: 'LV 1').")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Tooltip("Texto opcional para exibir os valores numéricos de XP (ex: '3 / 10').")]
    [SerializeField] private TextMeshProUGUI experienceText;

    [Header("Formatação de Texto")]
    [SerializeField] private string levelPrefix = "LV ";
    [SerializeField] private bool showExperienceNumbers = false;

    [Header("Animação")]
    [Tooltip("Velocidade de interpolação suave da barra de XP.")]
    [SerializeField] private float fillSpeed = 12f;

    private float targetFillAmount = 0f;
    private RectTransform fillRect;
    private int lastLevel = 1;
    private bool isInitialized = false;

    private void Awake()
    {
        if (fillImage != null)
        {
            fillRect = fillImage.rectTransform;
            SetFillScale(0f);
        }
    }

    private void Update()
    {
        if (fillImage == null) return;

        if (fillRect == null) return;

        float currentFill = fillRect.localScale.x;
        if (Mathf.Abs(currentFill - targetFillAmount) > 0.001f)
        {
            SetFillScale(Mathf.MoveTowards(currentFill, targetFillAmount, fillSpeed * Time.deltaTime));
        }
        else
        {
            SetFillScale(targetFillAmount);
        }
    }

    /// <summary>
    /// Atualiza os valores visuais da barra de experiência.
    /// </summary>
    public void SetExperience(int currentXp, int xpToNextLevel, int currentLevel)
    {
        float ratio = xpToNextLevel > 0 ? Mathf.Clamp01((float)currentXp / xpToNextLevel) : 1f;

        // Se subiu de nível, reseta o preenchimento para evitar lerp reverso esquisito
        if (currentLevel > lastLevel && isInitialized)
        {
            if (fillImage != null)
            {
                SetFillScale(0f);
            }
        }

        lastLevel = currentLevel;
        targetFillAmount = ratio;

        if (!isInitialized && fillImage != null)
        {
            SetFillScale(ratio);
            isInitialized = true;
        }

        UpdateTexts(currentXp, xpToNextLevel, currentLevel);
    }

    private void SetFillScale(float ratio)
    {
        if (fillRect == null) return;
        Vector3 scale = fillRect.localScale;
        scale.x = Mathf.Clamp01(ratio);
        fillRect.localScale = scale;
    }

    /// <summary>
    /// Notificação de subida de nível para efeitos visuais futuros ou animações.
    /// </summary>
    public void OnLevelUp(int newLevel)
    {
        lastLevel = newLevel;
        if (levelText != null)
        {
            levelText.text = $"{levelPrefix}{newLevel}";
        }
    }

    private void UpdateTexts(int currentXp, int xpToNextLevel, int currentLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"{levelPrefix}{currentLevel}";
        }

        if (experienceText != null)
        {
            if (showExperienceNumbers)
            {
                experienceText.text = $"{currentXp} / {xpToNextLevel}";
            }
            else
            {
                experienceText.text = string.Empty;
            }
        }
    }
}
