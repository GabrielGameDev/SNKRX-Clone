using UnityEngine;

/// <summary>
/// Barra de vida visual para segmentos da cobra (SegmentHealth).
/// Acompanha o segmento acima dele com rotação travada na horizontal,
/// torna-se visível temporariamente ao sofrer dano e desaparece suavemente com fade out.
/// </summary>
[DisallowMultipleComponent]
public class SegmentHealthBar : MonoBehaviour
{
    [Header("Alvo")]
    [Tooltip("Referência ao SegmentHealth monitorado. Se nulo, busca automaticamente no objeto ou pai.")]
    [SerializeField] private SegmentHealth targetHealth;

    [Header("Posicionamento")]
    [Tooltip("Offset relativo ao centro do segmento. A barra mantém rotação horizontal travada.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.65f, 0f);

    [Header("Dimensões")]
    [Tooltip("Largura (X) e altura (Y) da barra de vida no mundo.")]
    [SerializeField] private Vector2 barSize = new Vector2(0.8f, 0.12f);

    [Tooltip("Espaçamento de borda interna entre o fundo e a barra preenchida.")]
    [SerializeField] private float borderPadding = 0.02f;

    [Header("Temporizadores")]
    [Tooltip("Tempo em segundos que a barra permanece visível após receber dano.")]
    [SerializeField] private float visibleDuration = 2.0f;

    [Tooltip("Tempo de transição suave (fade out) para desaparecer após o término do tempo visível.")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Tooltip("Se verdadeiro, inicia oculta e só aparece ao levar dano.")]
    [SerializeField] private bool showOnlyWhenDamaged = true;

    [Tooltip("Se verdadeiro, oculta a barra imediatamente quando a vida estiver cheia (100%).")]
    [SerializeField] private bool hideWhenFullHealth = true;

    [Header("Cores")]
    [Tooltip("Cor do fundo da barra.")]
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    [Tooltip("Se verdadeiro, a cor da vida varia gradualmente entre Verde -> Amarelo -> Vermelho.")]
    [SerializeField] private bool useHealthGradient = true;

    [Tooltip("Cor da vida quando cheia / saudável.")]
    [SerializeField] private Color fullHealthColor = new Color(0.2f, 0.9f, 0.3f, 1f);

    [Tooltip("Cor da vida média (em torno de 50%).")]
    [SerializeField] private Color midHealthColor = new Color(0.95f, 0.85f, 0.2f, 1f);

    [Tooltip("Cor da vida baixa.")]
    [SerializeField] private Color lowHealthColor = new Color(0.95f, 0.25f, 0.25f, 1f);

    [Header("Renderização e Suavização")]
    [Tooltip("Velocidade de interpolação do preenchimento da barra ao tomar dano.")]
    [SerializeField] private float smoothFillSpeed = 12f;

    [Tooltip("Sorting layer dos renderers da barra.")]
    [SerializeField] private string sortingLayerName = "Default";

    [Tooltip("Order in layer do fundo da barra (o preenchimento usará +1).")]
    [SerializeField] private int sortingOrder = 50;

    [Header("Renderers Opcionais")]
    [Tooltip("Renderer do fundo. Se não fornecido, será criado automaticamente.")]
    [SerializeField] private SpriteRenderer backgroundRenderer;

    [Tooltip("Renderer do preenchimento. Se não fornecido, será criado automaticamente.")]
    [SerializeField] private SpriteRenderer fillRenderer;

    // Estado interno
    private float targetFillRatio = 1f;
    private float displayedFillRatio = 1f;
    private float lastKnownHealth = -1f;

    private float visibleTimer = 0f;
    private float fadeTimer = 0f;
    private float currentAlpha = 0f;
    private bool isVisible = false;

    private static Sprite defaultSprite;

    private void Awake()
    {
        EnsureRenderersCreated();

        if (targetHealth == null)
        {
            targetHealth = GetComponentInParent<SegmentHealth>() ?? GetComponent<SegmentHealth>();
        }
    }

    private void Start()
    {
        if (targetHealth != null)
        {
            Initialize(targetHealth);
        }
        else
        {
            SetRenderersActive(false);
        }
    }

    private void OnEnable()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged += HandleHealthChanged;
            targetHealth.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= HandleHealthChanged;
            targetHealth.OnDeath -= HandleDeath;
        }
    }

    /// <summary>
    /// Inicializa a barra vinculando ao SegmentHealth alvo.
    /// </summary>
    public void Initialize(SegmentHealth health)
    {
        if (targetHealth != null && targetHealth != health)
        {
            targetHealth.OnHealthChanged -= HandleHealthChanged;
            targetHealth.OnDeath -= HandleDeath;
        }

        targetHealth = health;

        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= HandleHealthChanged;
            targetHealth.OnDeath -= HandleDeath;

            targetHealth.OnHealthChanged += HandleHealthChanged;
            targetHealth.OnDeath += HandleDeath;

            float max = targetHealth.MaxHealth;
            float current = targetHealth.CurrentHealth;
            lastKnownHealth = current;

            targetFillRatio = max > 0f ? Mathf.Clamp01(current / max) : 1f;
            displayedFillRatio = targetFillRatio;

            if (showOnlyWhenDamaged && Mathf.Approximately(current, max))
            {
                currentAlpha = 0f;
                isVisible = false;
                SetRenderersActive(false);
            }
            else
            {
                ShowBar();
            }

            UpdateBarTransforms();
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        targetFillRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // Se sofreu dano ou mudou a vida
        if (current < lastKnownHealth || !showOnlyWhenDamaged)
        {
            ShowBar();
        }
        else if (hideWhenFullHealth && current >= max)
        {
            // Curou totalmente: inicia fade out ou oculta
            visibleTimer = 0f;
            fadeTimer = fadeOutDuration;
        }

        lastKnownHealth = current;
    }

    private void HandleDeath()
    {
        HideImmediate();
    }

    /// <summary>
    /// Exibe a barra imediatamente e inicia a contagem regressiva para sumir.
    /// </summary>
    public void ShowBar()
    {
        visibleTimer = visibleDuration;
        fadeTimer = 0f;
        currentAlpha = 1f;
        isVisible = true;

        SetRenderersActive(true);
        ApplyAlpha(1f);
    }

    /// <summary>
    /// Oculta a barra imediatamente.
    /// </summary>
    public void HideImmediate()
    {
        visibleTimer = 0f;
        fadeTimer = 0f;
        currentAlpha = 0f;
        isVisible = false;
        SetRenderersActive(false);
    }

    private void LateUpdate()
    {
        if (targetHealth == null) return;

        // Mantém a barra fixa acima do segmento sem girar quando a cobra vira
        transform.position = targetHealth.transform.position + offset;
        transform.rotation = Quaternion.identity;

        UpdateFillSmoothness();
        UpdateVisibilityTimers();
    }

    private void UpdateFillSmoothness()
    {
        if (Mathf.Abs(displayedFillRatio - targetFillRatio) > 0.001f)
        {
            displayedFillRatio = Mathf.MoveTowards(displayedFillRatio, targetFillRatio, smoothFillSpeed * Time.deltaTime);
            UpdateBarTransforms();
        }
    }

    private void UpdateVisibilityTimers()
    {
        if (!isVisible) return;

        if (visibleTimer > 0f)
        {
            visibleTimer -= Time.deltaTime;
            if (visibleTimer <= 0f)
            {
                visibleTimer = 0f;
                fadeTimer = fadeOutDuration;
            }
        }
        else if (fadeTimer > 0f)
        {
            fadeTimer -= Time.deltaTime;
            float normalizedFade = fadeOutDuration > 0f ? Mathf.Clamp01(fadeTimer / fadeOutDuration) : 0f;
            currentAlpha = normalizedFade;
            ApplyAlpha(currentAlpha);

            if (fadeTimer <= 0f)
            {
                fadeTimer = 0f;
                isVisible = false;
                SetRenderersActive(false);
            }
        }
    }

    private void UpdateBarTransforms()
    {
        if (backgroundRenderer == null || fillRenderer == null) return;

        // Fundo
        backgroundRenderer.transform.localPosition = Vector3.zero;
        backgroundRenderer.transform.localScale = new Vector3(barSize.x, barSize.y, 1f);

        // Preenchimento interno com border padding
        float maxInnerWidth = Mathf.Max(0.01f, barSize.x - (borderPadding * 2f));
        float innerHeight = Mathf.Max(0.01f, barSize.y - (borderPadding * 2f));
        float currentInnerWidth = maxInnerWidth * Mathf.Clamp01(displayedFillRatio);

        // Alinha a borda esquerda do preenchimento mantendo o encolhimento da direita para a esquerda
        float leftAlignedX = (-maxInnerWidth * 0.5f) + (currentInnerWidth * 0.5f);
        fillRenderer.transform.localPosition = new Vector3(leftAlignedX, 0f, 0f);
        fillRenderer.transform.localScale = new Vector3(Mathf.Max(0.0001f, currentInnerWidth), innerHeight, 1f);

        ApplyAlpha(currentAlpha);
    }

    private void ApplyAlpha(float alpha)
    {
        if (backgroundRenderer == null || fillRenderer == null) return;

        // Cor do fundo com alpha modulado
        Color bgCol = backgroundColor;
        bgCol.a = backgroundColor.a * alpha;
        backgroundRenderer.color = bgCol;

        // Cor da barra de vida com suporte a gradiente
        Color fillCol = CalculateHealthColor(displayedFillRatio);
        fillCol.a *= alpha;
        fillRenderer.color = fillCol;
    }

    private Color CalculateHealthColor(float ratio)
    {
        if (!useHealthGradient) return fullHealthColor;

        if (ratio > 0.5f)
        {
            float t = (ratio - 0.5f) * 2f;
            return Color.Lerp(midHealthColor, fullHealthColor, t);
        }
        else
        {
            float t = ratio * 2f;
            return Color.Lerp(lowHealthColor, midHealthColor, t);
        }
    }

    private void SetRenderersActive(bool active)
    {
        if (backgroundRenderer != null) backgroundRenderer.enabled = active;
        if (fillRenderer != null) fillRenderer.enabled = active;
    }

    private void EnsureRenderersCreated()
    {
        Sprite sprite = GetOrCreateDefaultSprite();

        // Background
        if (backgroundRenderer == null)
        {
            Transform bgTrans = transform.Find("Background");
            GameObject bgObj = bgTrans != null ? bgTrans.gameObject : new GameObject("Background");
            bgObj.transform.SetParent(transform, false);

            backgroundRenderer = bgObj.GetComponent<SpriteRenderer>();
            if (backgroundRenderer == null)
            {
                backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
            }

            backgroundRenderer.sprite = sprite;
            backgroundRenderer.sortingLayerName = sortingLayerName;
            backgroundRenderer.sortingOrder = sortingOrder;
            backgroundRenderer.color = backgroundColor;
        }

        // Fill
        if (fillRenderer == null)
        {
            Transform fillTrans = transform.Find("Fill");
            GameObject fillObj = fillTrans != null ? fillTrans.gameObject : new GameObject("Fill");
            fillObj.transform.SetParent(transform, false);

            fillRenderer = fillObj.GetComponent<SpriteRenderer>();
            if (fillRenderer == null)
            {
                fillRenderer = fillObj.AddComponent<SpriteRenderer>();
            }

            fillRenderer.sprite = sprite;
            fillRenderer.sortingLayerName = sortingLayerName;
            fillRenderer.sortingOrder = sortingOrder + 1;
            fillRenderer.color = fullHealthColor;
        }
    }

    private static Sprite GetOrCreateDefaultSprite()
    {
        if (defaultSprite == null)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[4] { Color.white, Color.white, Color.white, Color.white };
            texture.SetPixels(pixels);
            texture.Apply();

            defaultSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }

        return defaultSprite;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (backgroundRenderer != null && fillRenderer != null)
        {
            backgroundRenderer.sortingLayerName = sortingLayerName;
            backgroundRenderer.sortingOrder = sortingOrder;
            fillRenderer.sortingLayerName = sortingLayerName;
            fillRenderer.sortingOrder = sortingOrder + 1;
            UpdateBarTransforms();
        }
    }
#endif
}
