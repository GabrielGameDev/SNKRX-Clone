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

    [Tooltip("Text showing the assigned color name (for example, 'Blue Orb').")]
    [SerializeField] private TextMeshProUGUI colorNameText;

    [Tooltip("Título do upgrade ('? ? ?' se desconhecido ou o nome real se descoberto).")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Descrição detalhada do efeito.")]
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Tooltip("Status badge (for example, 'UNKNOWN' or 'LEVEL 2').")]
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
        string colorName = state.AssignedColor.colorName;

        // Keep the cards readable in both the scene and dynamically generated layouts.
        RectTransform cardRect = transform as RectTransform;
        if (cardRect != null) cardRect.sizeDelta = new Vector2(320f, 480f);
        Image background = GetComponent<Image>();
        if (background != null) background.color = new Color(0.96f, 0.95f, 0.91f, 1f);

        if (colorOrb != null)
        {
            if (colorOrb.sprite == null)
            {
                colorOrb.sprite = GetOrCreateCircleSprite();
            }
            colorOrb.color = runColor;
            SetTextRect(colorOrb.rectTransform, new Vector2(0f, 135f), new Vector2(84f, 84f));
        }

        if (cardBorder != null)
        {
            cardBorder.color = new Color(runColor.r, runColor.g, runColor.b, 0.9f);
        }

        if (colorNameText != null)
        {
            colorNameText.text = $"{colorName} Orb";
            colorNameText.color = runColor;
            // colorNameText.fontSize = 22f;
            // SetTextRect(colorNameText.rectTransform, new Vector2(0f, 77f), new Vector2(280f, 34f));
        }

        if (!state.IsDiscovered)
        {
            // Estado Oculto / Mistério
            if (titleText != null)
            {
                titleText.text = "? ? ?";
                titleText.color = Color.black;
                // titleText.fontSize = 25f;
                // SetTextRect(titleText.rectTransform, new Vector2(0f, 27f), new Vector2(290f, 60f));
            }

            if (descriptionText != null)
            {
                descriptionText.text = $"Mysterious Effect!\n\nChoose this upgrade to reveal its power for this run and add a {colorName.ToLower()} segment to the snake.";
                descriptionText.color = Color.black;
                // descriptionText.fontSize = 18f;
                // SetTextRect(descriptionText.rectTransform, new Vector2(0f, -88f), new Vector2(280f, 190f));
            }

            if (statusBadgeText != null)
            {
                statusBadgeText.text = "UNKNOWN";
                statusBadgeText.color = Color.black;
                // statusBadgeText.fontSize = 16f;
                // SetTextRect(statusBadgeText.rectTransform, new Vector2(0f, -205f), new Vector2(260f, 32f));
            }
        }
        else
        {
            // Estado Revelado
            if (titleText != null)
            {
                titleText.text = state.DisplayName;
                titleText.color = Color.black;
                // titleText.fontSize = 25f;
                // SetTextRect(titleText.rectTransform, new Vector2(0f, 27f), new Vector2(290f, 60f));
            }

            if (descriptionText != null)
            {
                descriptionText.text = state.DiscoveredDescription;
                descriptionText.color = Color.black;
                // descriptionText.fontSize = 18f;
                // SetTextRect(descriptionText.rectTransform, new Vector2(0f, -88f), new Vector2(280f, 190f));
            }

            if (statusBadgeText != null)
            {
                statusBadgeText.text = state.Level > 0 ? $"LEVEL {state.Level + 1}" : "DISCOVERED";
                statusBadgeText.color = Color.black;
                // statusBadgeText.fontSize = 16f;
                // SetTextRect(statusBadgeText.rectTransform, new Vector2(0f, -205f), new Vector2(260f, 32f));
            }
        }
    }

    public void RefreshIfStateMatches(RunUpgradeState state)
    {
        if (currentState == state)
            Setup(currentState, onSelectedCallback);
    }

    private static void SetTextRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void HandleClicked()
    {
        if (currentState != null)
        {
            onSelectedCallback?.Invoke(currentState);
        }
    }
}
