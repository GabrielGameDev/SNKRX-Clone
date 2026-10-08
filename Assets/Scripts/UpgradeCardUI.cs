using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente de uma carta individual de upgrade na tela de seleção.
/// Exibe a cor da run, o título (? ? ? se desconhecido ou o nome real se descoberto),
/// descrição e botão de escolha com feedback visual.
/// </summary>
public class UpgradeCardUI : MonoBehaviour
{
    [Header("Elementos Visuais")]
    [Tooltip("Imagem da esfera/orbe que representa a cor da run.")]
    [SerializeField] private Image colorOrb;

    [Tooltip("Borda ou fundo com tintura da cor da run.")]
    [SerializeField] private Image cardBorder;

    [Tooltip("Texto com o nome da cor (ex: 'Esfera Azul').")]
    [SerializeField] private TextMeshProUGUI colorNameText;

    [Tooltip("Título do upgrade ('? ? ?' se desconhecido ou o nome real se descoberto).")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Descrição detalhada do efeito.")]
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Tooltip("Badge de status (ex: 'DESCONHECIDO' ou 'NÍVEL 2').")]
    [SerializeField] private TextMeshProUGUI statusBadgeText;

    [Tooltip("Botão interativo de seleção da carta.")]
    [SerializeField] private Button selectButton;

    private RunUpgradeState currentState;
    private Action<RunUpgradeState> onSelectedCallback;
    private static Sprite circleSpriteCache;

    public Image ColorOrb { get => colorOrb; set => colorOrb = value; }
    public Image CardBorder { get => cardBorder; set => cardBorder = value; }
    public TextMeshProUGUI ColorNameText { get => colorNameText; set => colorNameText = value; }
    public TextMeshProUGUI TitleText { get => titleText; set => titleText = value; }
    public TextMeshProUGUI DescriptionText { get => descriptionText; set => descriptionText = value; }
    public TextMeshProUGUI StatusBadgeText { get => statusBadgeText; set => statusBadgeText = value; }
    public Button SelectButton { get => selectButton; set => selectButton = value; }

    private void Awake()
    {
        if (selectButton == null)
        {
            selectButton = GetComponent<Button>();
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(HandleClicked);
        }
    }

    private void OnDestroy()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleClicked);
        }
    }

    /// <summary>
    /// Retorna ou gera proceduralmente um sprite circular suave para o orbe de cor.
    /// </summary>
    public static Sprite GetOrCreateCircleSprite()
    {
        if (circleSpriteCache != null) return circleSpriteCache;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = (size - 1) * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        circleSpriteCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return circleSpriteCache;
    }

    /// <summary>
    /// Configura os dados visuais da carta de acordo com o estado do upgrade.
    /// </summary>
    public void Setup(RunUpgradeState state, Action<RunUpgradeState> onSelected)
    {
        currentState = state;
        onSelectedCallback = onSelected;

        Color runColor = state.AssignedColor.color;
        string hexColor = state.AssignedColor.hexCode;
        string colorName = state.AssignedColor.colorName;

        if (colorOrb != null)
        {
            if (colorOrb.sprite == null)
            {
                colorOrb.sprite = GetOrCreateCircleSprite();
            }
            colorOrb.color = runColor;
        }

        if (cardBorder != null)
        {
            cardBorder.color = new Color(runColor.r, runColor.g, runColor.b, 0.9f);
        }

        if (colorNameText != null)
        {
            colorNameText.text = $"<color={hexColor}>Esfera {colorName}</color>";
        }

        if (!state.IsDiscovered)
        {
            // Estado Oculto / Mistério
            if (titleText != null)
            {
                titleText.text = "? ? ?";
            }

            if (descriptionText != null)
            {
                descriptionText.text = $"Efeito Misterioso!\n\nEscolha para desvendar este poder nesta run e criar uma bolinha {colorName.ToLower()} no corpo da cobra.";
            }

            if (statusBadgeText != null)
            {
                statusBadgeText.text = "DESCONHECIDO";
                statusBadgeText.color = new Color(0.9f, 0.9f, 0.9f, 0.6f);
            }
        }
        else
        {
            // Estado Revelado
            if (titleText != null)
            {
                titleText.text = state.DisplayName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = state.DiscoveredDescription;
            }

            if (statusBadgeText != null)
            {
                statusBadgeText.text = state.Level > 0 ? $"NÍVEL {state.Level + 1}" : "DESCOBERTO";
                statusBadgeText.color = runColor;
            }
        }
    }

    private void HandleClicked()
    {
        if (currentState != null)
        {
            onSelectedCallback?.Invoke(currentState);
        }
    }
}
