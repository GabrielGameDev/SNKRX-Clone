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

    [Tooltip("Tempo de invulnerabilidade temporária após sofrer um golpe.")]
    [SerializeField] private float invulnerabilityDuration = 0.2f;

    [Header("Visual Feedback")]
    [Tooltip("SpriteRenderer do segmento para efeito de piscar ao receber dano.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Cor aplicada durante o flash de dano.")]
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.3f, 0.3f, 1f);

    private float currentHealth;
    private float invulnerabilityTimer;
    private Color originalColor = Color.white;
    private Coroutine flashCoroutine;
    private SnakeSegment snakeSegment;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;
    public bool IsHead => isHead;

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
    }

    private void Update()
    {
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Aplica dano a este segmento específico da cobra.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (IsDead || invulnerabilityTimer > 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        invulnerabilityTimer = invulnerabilityDuration;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Feedback visual
        if (spriteRenderer != null && gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();

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
