using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal de interface do usuário que abre ao subir de nível.
/// Pausa o jogo (Time.timeScale = 0), apresenta 3 cartas de upgrades aleatórios,
/// processa a escolha do jogador e retoma o tempo (Time.timeScale = 1).
/// Se nenhum layout de UI for configurado no Inspector, constrói automaticamente
/// uma interface roguelike moderna e elegante no Canvas.
/// </summary>
public class UpgradeSelectionUI : MonoBehaviour
{
    public static UpgradeSelectionUI Instance { get; private set; }

    [Header("Estrutura do Painel")]
    [Tooltip("Objeto raiz do painel modal que será ativado/desativado.")]
    [SerializeField] private GameObject modalRoot;

    [Tooltip("Header text (for example, 'LEVEL UP!').")]
    [SerializeField] private TextMeshProUGUI headerText;

    [Tooltip("Texto do subtítulo.")]
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Cartas")]
    [Tooltip("As 3 cartas de upgrade fixas ou slots dentro do painel.")]
    [SerializeField] private UpgradeCardUI[] cardSlots;

    [Tooltip("Prefab opcional da carta caso sejam geradas dinamicamente.")]
    [SerializeField] private UpgradeCardUI cardPrefab;

    [Tooltip("Container pai onde as cartas geradas dinamicamente serão instanciadas.")]
    [SerializeField] private Transform cardsContainer;

    private int pendingLevelUps = 0;
    private bool isSelecting = false;
    private bool isResolvingChoice = false;
    private readonly List<UpgradeCardUI> spawnedCards = new List<UpgradeCardUI>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ConfigureHeaderText();

        if (modalRoot == null && (cardSlots == null || cardSlots.Length == 0))
        {
            BuildDynamicUI();
        }

        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    private void ConfigureHeaderText()
    {
        if (headerText != null)
        {
            headerText.text = "LEVEL UP!";
        }

        if (subtitleText != null)
        {
            subtitleText.text = "Choose an upgrade for your snake:";
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        pendingLevelUps++;

        if (!isSelecting)
        {
            ShowNextSelection();
        }
    }

    /// <summary>
    /// Abre a tela de escolha para o próximo level-up da fila e pausa o jogo.
    /// </summary>
    private void ShowNextSelection()
    {
        if (pendingLevelUps <= 0)
        {
            CloseModal();
            return;
        }

        if (UpgradeManager.Instance == null)
        {
            Debug.LogWarning("[UpgradeSelectionUI] UpgradeManager não encontrado na cena!");
            pendingLevelUps = 0;
            return;
        }

        isSelecting = true;
        isResolvingChoice = false;
        SetCardsInteractable(true);
        Time.timeScale = 0f; // Pausa o jogo durante a escolha

        if (modalRoot != null)
        {
            modalRoot.SetActive(true);
        }

        // Obtém 3 escolhas aleatórias
        List<RunUpgradeState> choices = UpgradeManager.Instance.GetRandomChoices(3);

        // Se tivermos slots fixos configurados no Inspector ou criados dinamicamente
        if (cardSlots != null && cardSlots.Length >= 3)
        {
            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (i < choices.Count)
                {
                    cardSlots[i].gameObject.SetActive(true);
                    cardSlots[i].Setup(choices[i], OnCardSelected);
                }
                else
                {
                    cardSlots[i].gameObject.SetActive(false);
                }
            }
        }
        else if (cardPrefab != null && cardsContainer != null)
        {
            // Geração dinâmica via prefab
            ClearSpawnedCards();

            for (int i = 0; i < choices.Count; i++)
            {
                UpgradeCardUI card = Instantiate(cardPrefab, cardsContainer);
                card.Setup(choices[i], OnCardSelected);
                spawnedCards.Add(card);
            }
        }
    }

    /// <summary>
    /// Chamado quando o jogador clica em uma das 3 cartas.
    /// </summary>
    private void OnCardSelected(RunUpgradeState selectedState)
    {
        if (isResolvingChoice || selectedState == null || UpgradeManager.Instance == null) return;

        bool wasUndiscovered = !selectedState.IsDiscovered;

        // Apply the effect and reveal its identity.
        UpgradeManager.Instance.SelectUpgrade(selectedState.Type);

        if (wasUndiscovered)
        {
            isResolvingChoice = true;
            SetCardsInteractable(false);
            RefreshSelectedCard(selectedState);
            StartCoroutine(CompleteUndiscoveredChoice());
            return;
        }

        CompleteChoiceAndContinue();
    }

    private IEnumerator CompleteUndiscoveredChoice()
    {
        // Keep the selected card visible long enough to read its newly revealed effect.
        yield return new WaitForSecondsRealtime(2f);
        CompleteChoiceAndContinue();
    }

    private void RefreshSelectedCard(RunUpgradeState selectedState)
    {
        foreach (UpgradeCardUI card in GetActiveCards())
        {
            if (card != null && card.gameObject.activeInHierarchy && card.TitleText != null &&
                card.TitleText.text == "? ? ?")
            {
                // Match by the selected state through each card's own refresh callback below.
                card.RefreshIfStateMatches(selectedState);
            }
        }
    }

    private void CompleteChoiceAndContinue()
    {
        pendingLevelUps--;

        if (pendingLevelUps > 0)
        {
            // Se ainda tem mais níveis pendentes, abre imediatamente a próxima escolha
            ShowNextSelection();
        }
        else
        {
            CloseModal();
        }
    }

    private void SetCardsInteractable(bool interactable)
    {
        foreach (UpgradeCardUI card in GetActiveCards())
            if (card != null && card.SelectButton != null)
                card.SelectButton.interactable = interactable;
    }

    private IEnumerable<UpgradeCardUI> GetActiveCards()
    {
        if (cardSlots != null)
        {
            foreach (UpgradeCardUI card in cardSlots)
                if (card != null) yield return card;
        }

        foreach (UpgradeCardUI card in spawnedCards)
            if (card != null) yield return card;
    }

    /// <summary>
    /// Fecha o modal e retoma o tempo normal do jogo.
    /// </summary>
    private void CloseModal()
    {
        isSelecting = false;
        Time.timeScale = 1f;

        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }

        ClearSpawnedCards();
    }

    private void ClearSpawnedCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
            {
                Destroy(spawnedCards[i].gameObject);
            }
        }
        spawnedCards.Clear();
    }

    /// <summary>
    /// Constrói programaticamente a interface modal roguelike com 3 cartas caso não exista no prefab/cena.
    /// </summary>
    private void BuildDynamicUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogWarning("[UpgradeSelectionUI] Nenhum Canvas encontrado para criar o modal de upgrades!");
            return;
        }

        // Fundo escuro modal cobrindo toda a tela
        modalRoot = new GameObject("UpgradeModal_Root", typeof(RectTransform), typeof(Image));
        modalRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRt = modalRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        Image rootBg = modalRoot.GetComponent<Image>();
        rootBg.color = new Color(0.04f, 0.04f, 0.08f, 0.92f); // Escurece o fundo

        // Painel central de conteúdo
        GameObject contentPanel = new GameObject("ContentPanel", typeof(RectTransform));
        contentPanel.transform.SetParent(modalRoot.transform, false);
        RectTransform panelRt = contentPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(1080f, 680f);

        // Header Title
        GameObject headerGo = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerGo.transform.SetParent(contentPanel.transform, false);
        RectTransform headerRt = headerGo.GetComponent<RectTransform>();
        headerRt.anchoredPosition = new Vector2(0f, 280f);
        headerRt.sizeDelta = new Vector2(900f, 64f);

        headerText = headerGo.GetComponent<TextMeshProUGUI>();
        headerText.text = "LEVEL UP!";
        headerText.fontSize = 42;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = new Color(1f, 0.85f, 0.2f); // Dourado
        headerText.alignment = TextAlignmentOptions.Center;

        // Subtitle
        GameObject subGo = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGo.transform.SetParent(contentPanel.transform, false);
        RectTransform subRt = subGo.GetComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0f, 220f);
        subRt.sizeDelta = new Vector2(900f, 45f);

        subtitleText = subGo.GetComponent<TextMeshProUGUI>();
        subtitleText.text = "Choose an upgrade for your snake:";
        subtitleText.fontSize = 24;
        subtitleText.color = new Color(0.85f, 0.85f, 0.9f, 0.8f);
        subtitleText.alignment = TextAlignmentOptions.Center;

        // Container das 3 Cartas
        GameObject containerGo = new GameObject("CardsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        containerGo.transform.SetParent(contentPanel.transform, false);
        cardsContainer = containerGo.transform;

        RectTransform contRt = containerGo.GetComponent<RectTransform>();
        contRt.anchoredPosition = new Vector2(0f, -65f);
        contRt.sizeDelta = new Vector2(1024f, 500f);

        HorizontalLayoutGroup hlg = containerGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        cardSlots = new UpgradeCardUI[3];
        for (int i = 0; i < 3; i++)
        {
            cardSlots[i] = CreateDynamicCard(cardsContainer, i);
        }
    }

    private UpgradeCardUI CreateDynamicCard(Transform parent, int index)
    {
        GameObject cardGo = new GameObject($"Card_{index}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UpgradeCardUI));
        cardGo.transform.SetParent(parent, false);

        RectTransform cardRt = cardGo.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(320f, 480f);

        Image bgImage = cardGo.GetComponent<Image>();
        bgImage.color = new Color(0.96f, 0.95f, 0.91f, 1f);

        Button btn = cardGo.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;

        UpgradeCardUI card = cardGo.GetComponent<UpgradeCardUI>();
        card.SelectButton = btn;

        // Borda Colorida
        GameObject borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        borderGo.transform.SetParent(cardGo.transform, false);
        RectTransform borderRt = borderGo.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.sizeDelta = Vector2.zero;
        Image borderImg = borderGo.GetComponent<Image>();
        borderImg.color = new Color(1f, 1f, 1f, 0.15f);
        borderImg.raycastTarget = false;
        card.CardBorder = borderImg;

        // Orbe de Cor (Bolinha)
        GameObject orbGo = new GameObject("ColorOrb", typeof(RectTransform), typeof(Image));
        orbGo.transform.SetParent(cardGo.transform, false);
        RectTransform orbRt = orbGo.GetComponent<RectTransform>();
        orbRt.anchoredPosition = new Vector2(0f, 135f);
        orbRt.sizeDelta = new Vector2(84f, 84f);
        Image orbImg = orbGo.GetComponent<Image>();
        orbImg.sprite = UpgradeCardUI.GetOrCreateCircleSprite();
        orbImg.raycastTarget = false;
        card.ColorOrb = orbImg;

        // Nome da Cor
        GameObject colorNameGo = new GameObject("ColorName", typeof(RectTransform), typeof(TextMeshProUGUI));
        colorNameGo.transform.SetParent(cardGo.transform, false);
        RectTransform cnRt = colorNameGo.GetComponent<RectTransform>();
        cnRt.anchoredPosition = new Vector2(0f, 77f);
        cnRt.sizeDelta = new Vector2(280f, 34f);
        TextMeshProUGUI cnText = colorNameGo.GetComponent<TextMeshProUGUI>();
        cnText.fontSize = 22;
        cnText.fontStyle = FontStyles.Bold;
        cnText.alignment = TextAlignmentOptions.Center;
        cnText.raycastTarget = false;
        card.ColorNameText = cnText;

        // Título do Upgrade
        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(cardGo.transform, false);
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 27f);
        titleRt.sizeDelta = new Vector2(290f, 60f);
        TextMeshProUGUI titleText = titleGo.GetComponent<TextMeshProUGUI>();
        titleText.fontSize = 25;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.raycastTarget = false;
        card.TitleText = titleText;

        // Descrição do Upgrade
        GameObject descGo = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGo.transform.SetParent(cardGo.transform, false);
        RectTransform descRt = descGo.GetComponent<RectTransform>();
        descRt.anchoredPosition = new Vector2(0f, -88f);
        descRt.sizeDelta = new Vector2(280f, 190f);
        TextMeshProUGUI descText = descGo.GetComponent<TextMeshProUGUI>();
        descText.fontSize = 18;
        descText.lineSpacing = 3f;
        descText.color = Color.black;
        descText.alignment = TextAlignmentOptions.Center;
        descText.textWrappingMode = TextWrappingModes.Normal;
        descText.raycastTarget = false;
        card.DescriptionText = descText;

        // Badge de Status
        GameObject badgeGo = new GameObject("StatusBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeGo.transform.SetParent(cardGo.transform, false);
        RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
        badgeRt.anchoredPosition = new Vector2(0f, -205f);
        badgeRt.sizeDelta = new Vector2(260f, 32f);
        TextMeshProUGUI badgeText = badgeGo.GetComponent<TextMeshProUGUI>();
        badgeText.fontSize = 16;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.raycastTarget = false;
        card.StatusBadgeText = badgeText;

        return card;
    }
}
