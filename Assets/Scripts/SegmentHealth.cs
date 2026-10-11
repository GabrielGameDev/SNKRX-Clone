using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Controla a vida individual de cada segmento da cobra (ou da cabeça).
/// Quando um segmento do corpo perde toda a vida, ele é destruído e a cobra se reajusta.
/// Se a cabeça perder toda a vida, o jogador é derrotado.
/// </summary>
[DisallowMultipleComponent]
public class SegmentHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Pontos de vida máximos deste segmento.")]
    [SerializeField] private float maxHealth = 3f;

    [Tooltip("Se verdadeiro, este componente pertence à cabeça principal da cobra.")]
    [SerializeField] private bool isHead = false;

    [Tooltip("Tempo de invulnerabilidade temporária após sofrer um golpe (em segundos).")]
    [SerializeField] private float invulnerabilityDuration = 1.0f;

    [Tooltip("Dano causado e recebido ao colidir com outro segmento não adjacente.")]
    [SerializeField, Min(0f)] private float collisionDamage = 1f;

    [Header("Visual Feedback")]
    [Tooltip("SpriteRenderer do segmento para efeito de piscar ao receber dano.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Cor aplicada durante o flash inicial de dano.")]
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("Intervalo entre cada piscada durante a invencibilidade.")]
    [SerializeField] private float blinkInterval = 0.08f;

    [Tooltip("Opacidade reduzida durante o efeito de piscar.")]
    [Range(0f, 1f)]
    [SerializeField] private float invulnerableAlpha = 0.3f;

    [Header("Health Bar")]
    [Tooltip("Barra de vida visual opcional deste segmento. Se não definida e autoCreateHealthBar estiver ativa, será criada automaticamente.")]
    [SerializeField] private SegmentHealthBar healthBar;

    [Tooltip("Se verdadeiro, cria automaticamente a barra de vida como filha deste objeto se nenhuma for especificada.")]
    [SerializeField] private bool autoCreateHealthBar = true;

    [Header("Death Feedback")]
    [SerializeField] private ExplosionEffect deathEffectPrefab;

    private float currentHealth;
    private float invulnerabilityTimer;
    private Color originalColor = Color.white;
    private Coroutine flashCoroutine;
    private SnakeSegment snakeSegment;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;
    public bool IsHead => isHead;
    public bool IsInvulnerable => invulnerabilityTimer > 0f;
    public float InvulnerabilityDuration { get => invulnerabilityDuration; set => invulnerabilityDuration = Mathf.Max(0f, value); }
    public SegmentHealthBar HealthBar => healthBar;

    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
        snakeSegment = GetComponent<SnakeSegment>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        SetupHealthBar();
    }

    private void SetupHealthBar()
    {
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<SegmentHealthBar>();

            if (healthBar == null && autoCreateHealthBar)
            {
                GameObject barObj = new GameObject("HealthBar");
                barObj.transform.SetParent(transform, false);
                healthBar = barObj.AddComponent<SegmentHealthBar>();
            }
        }

        if (healthBar != null)
        {
            healthBar.Initialize(this);
        }
    }

    private void OnDisable()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SegmentHealth otherSegment = other.GetComponent<SegmentHealth>();
        if (otherSegment == null) otherSegment = other.GetComponentInParent<SegmentHealth>();
        if (otherSegment == null || otherSegment == this || otherSegment.IsDead || IsDead) return;
        if (AreAdjacentParts(otherSegment)) return;

        if (collisionDamage > 0f) TakeDamage(collisionDamage);
        if (otherSegment.collisionDamage > 0f) otherSegment.TakeDamage(otherSegment.collisionDamage);
    }

    private bool AreAdjacentParts(SegmentHealth other)
    {
        SnakeSegment thisPart = GetComponent<SnakeSegment>();
        SnakeSegment otherPart = other.GetComponent<SnakeSegment>();

        if (thisPart != null && otherPart != null && thisPart.SnakeOwner == otherPart.SnakeOwner)
            return Mathf.Abs(thisPart.SegmentIndex - otherPart.SegmentIndex) <= 1;

        if (isHead && otherPart != null && otherPart.SnakeOwner == SnakeMovement.Instance)
            return otherPart.SegmentIndex == 0;

        if (other.isHead && thisPart != null && thisPart.SnakeOwner == SnakeMovement.Instance)
            return thisPart.SegmentIndex == 0;

        return false;
    }

    /// <summary>
    /// Define a cor base do segmento e atualiza sua renderização e cor de retorno pós-dano.
    /// </summary>
    public void SetBaseColor(Color color)
    {
        originalColor = color;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// Aumenta a vida máxima do segmento e opcionalmente cura a mesma quantia.
    /// </summary>
    public void IncreaseMaxHealth(float amount, bool healAmount = true)
    {
        maxHealth += amount;
        if (healAmount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Cura pontos de vida deste segmento até o limite de vida máxima.
    /// </summary>
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0f)
            {
                invulnerabilityTimer = 0f;
                ResetVisual();
            }
        }
    }

    /// <summary>
    /// Aplica dano a este segmento específico da cobra.
    /// Se estiver no período de invencibilidade, ignora o dano.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (IsDead || invulnerabilityTimer > 0f) return;

        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (currentHealth < previousHealth)
        {
            SfxPlayer.PlayHit();
            SfxPlayer.PlayDamageFreeze();
        }
        invulnerabilityTimer = invulnerabilityDuration;
        GameEvents.TriggerPlayerDamageReceived(previousHealth - currentHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Feedback visual contínuo durante a invencibilidade
        if (spriteRenderer != null && gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(InvulnerabilityBlinkRoutine());
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private IEnumerator InvulnerabilityBlinkRoutine()
    {
        if (spriteRenderer == null) yield break;

        // Flash inicial de dano
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(0.1f);

        if (spriteRenderer == null) yield break;

        // Piscar enquanto durar a invencibilidade
        bool isDimmed = false;
        Color dimmedColor = new Color(originalColor.r, originalColor.g, originalColor.b, invulnerableAlpha);

        while (invulnerabilityTimer > 0f)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isDimmed ? originalColor : dimmedColor;
            }
            isDimmed = !isDimmed;
            yield return new WaitForSeconds(blinkInterval);
        }

        ResetVisual();
    }

    private void ResetVisual()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void Die()
    {
        ResetVisual();
        OnDeath?.Invoke();

        if (!isHead)
        {
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            SfxPlayer.PlayExplosion();
            SfxPlayer.PlaySegmentDeathSlow();
        }

        if (isHead)
        {
            Debug.Log("<color=red>A cabeça da cobra foi derrotada! Game Over.</color>");
            GameEvents.TriggerPlayerDied();
            // Desativa movimentação ou trata fim de jogo
            gameObject.SetActive(false);
        }
        else
        {
            // Segmento do corpo: remove-se da cobra e reajusta o corpo
            if (SnakeMovement.Instance != null && snakeSegment != null)
            {
                SnakeMovement.Instance.RemoveSegment(snakeSegment);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Restaura a vida do segmento ao valor máximo.
    /// </summary>
    public void HealToFull()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
