using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla os pontos de vida de um inimigo e mantém um registro estático
/// de todos os inimigos ativos na cena, permitindo busca rápida sem alocação de GC.
/// </summary>
[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    /// <summary>
    /// Lista estática com todos os inimigos ativos no momento.
    /// Atualizada automaticamente em OnEnable e OnDisable.
    /// </summary>
    public static readonly List<EnemyHealth> AllEnemies = new List<EnemyHealth>();

    [Header("Health Settings")]
    [Tooltip("Vida máxima inicial do inimigo.")]
    [SerializeField] private float maxHealth = 10f;

    [Header("Health Bar")]
    [Tooltip("Barra de vida do inimigo. Se vazia, será criada automaticamente.")]
    [SerializeField] private SegmentHealthBar healthBar;

    [SerializeField] private bool autoCreateHealthBar = true;

    [Header("Damage Feedback")]
    [SerializeField, Min(0f)] private float hitFlashDuration = 0.1f;

    [Header("Death Feedback")]
    [SerializeField] private GameObject deathEffectPrefab;

    [Header("XP Drop")]
    [Tooltip("Prefab do item de XP solto quando este inimigo morre. Configure no Inspector.")]
    [SerializeField] private XpPickup xpPickupPrefab;

    [Tooltip("Quantidade de XP concedida ao coletar o item deste inimigo.")]
    [Min(1)]
    [SerializeField] private int xpDropAmount = 1;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;

    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action OnDeath;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Coroutine hitFlashCoroutine;

    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
        CacheSpriteRenderers();
        SetupHealthBar();
    }

    private void CacheSpriteRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
    }

    protected virtual void OnDestroy()
    {
        RestoreSpriteColors();
    }

    private void RestoreSpriteColors()
    {
        if (spriteRenderers == null || originalColors == null) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null) spriteRenderers[i].color = originalColors[i];
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null) spriteRenderers[i].color = Color.white;
        }

        yield return new WaitForSeconds(hitFlashDuration);
        RestoreSpriteColors();
        hitFlashCoroutine = null;
    }

    private void FlashOnHit()
    {
        if (hitFlashDuration <= 0f || spriteRenderers == null || !gameObject.activeInHierarchy) return;

        if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private void SetupHealthBar()
    {
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<SegmentHealthBar>();
            if (healthBar == null && autoCreateHealthBar)
            {
                var barObject = new GameObject("HealthBar");
                barObject.transform.SetParent(transform, false);
                healthBar = barObject.AddComponent<SegmentHealthBar>();
            }
        }

        if (healthBar != null)
        {
            healthBar.Initialize(this);
        }
    }

    /// <summary>
    /// Ajusta a vida máxima do inimigo (usado por ondas progressivas).
    /// Deve ser chamado após o Awake, imediatamente após o Instantiate.
    /// </summary>
    public void SetMaxHealth(float newMax)
    {
        maxHealth = newMax;
        CurrentHealth = newMax;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    protected virtual void OnEnable()
    {
        if (!AllEnemies.Contains(this))
        {
            AllEnemies.Add(this);
        }
    }

    protected virtual void OnDisable()
    {
        AllEnemies.Remove(this);

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }

        RestoreSpriteColors();
    }

    /// <summary>
    /// Aplica dano ao inimigo. Caso a vida chegue a 0 ou menos, dispara a morte.
    /// </summary>
    public virtual void TakeDamage(float amount)
    {
        if (IsDead) return;

        float previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        if (CurrentHealth < previousHealth)
        {
            FlashOnHit();
            EnemyDamageNumber.Spawn(transform.position, previousHealth - CurrentHealth);
            SfxPlayer.PlayHit();
        }
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Executado quando a vida é zerada. Pode ser sobrescrito por subclasses para efeitos/sons.
    /// </summary>
    protected virtual void Die()
    {
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        SfxPlayer.PlayExplosion();
        OnDeath?.Invoke();
        GameEvents.TriggerEnemyKilled(this);

        if (xpPickupPrefab != null)
        {
            XpPickup pickup = Instantiate(xpPickupPrefab, transform.position, Quaternion.identity);
            pickup.Initialize(xpDropAmount);
        }

        Destroy(gameObject);
    }
}
