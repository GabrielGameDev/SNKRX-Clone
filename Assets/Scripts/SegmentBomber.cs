using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Componente anexado a um segmento da cobra (SnakeSegment) que gera explosões
/// periódicas em área, danificando todos os inimigos dentro do raio configurado.
/// Antes de cada explosão, exibe um aviso visual piscando o SpriteRenderer do segmento.
/// </summary>
[DisallowMultipleComponent]
public class SegmentBomber : MonoBehaviour
{
    [Header("Explosion Settings")]
    [Tooltip("Intervalo de tempo em segundos entre cada explosão.")]
    [SerializeField] private float explosionInterval = 3f;

    [Tooltip("Raio da área de dano da explosão.")]
    [SerializeField] private float explosionRadius = 3f;

    [Tooltip("Dano causado a cada inimigo atingido pela explosão.")]
    [SerializeField] private float explosionDamage = 2f;

    [Tooltip("Prefab do efeito visual da explosão. Reutiliza o ExplosionEffect existente.")]
    [SerializeField] private ExplosionEffect explosionEffectPrefab;

    [Header("Warning Flash")]
    [Tooltip("Duração do aviso visual (flash) antes de cada explosão, em segundos.")]
    [SerializeField] private float warningDuration = 0.4f;

    [Tooltip("Intervalo em segundos entre cada piscada durante o aviso.")]
    [SerializeField] private float flashSpeed = 0.08f;

    [Tooltip("Cor do flash de aviso antes da explosão.")]
    [SerializeField] private Color warningColor = new Color(1f, 0.35f, 0f); // laranja

    [Header("Debug")]
    [Tooltip("Exibe o raio de explosão no editor ao selecionar o objeto.")]
    [SerializeField] private bool showRangeGizmo = true;

    // ── Propriedades públicas (para upgrades futuros) ──────────────────────────
    public float ExplosionInterval
    {
        get => explosionInterval;
        set => explosionInterval = Mathf.Max(0.5f, value);
    }

    public float ExplosionRadius
    {
        get => explosionRadius;
        set => explosionRadius = Mathf.Max(0.1f, value);
    }

    public float ExplosionDamage
    {
        get => explosionDamage;
        set => explosionDamage = Mathf.Max(0f, value);
    }

    public float EffectiveExplosionRadius => explosionRadius * (UpgradeManager.Instance != null ? UpgradeManager.Instance.RangeMultiplier : 1f);
    public float EffectiveExplosionDamage => explosionDamage * (UpgradeManager.Instance != null ? UpgradeManager.Instance.GlobalDamageMultiplier : 1f);

    /// <summary>
    /// Define a cor base do bomber e atualiza sua renderização e cor de retorno pós-flash.
    /// </summary>
    public void SetBaseColor(Color color)
    {
        _originalColor = color;
        if (_sr != null)
        {
            _sr.color = color;
        }
    }

    // ── Internos ───────────────────────────────────────────────────────────────
    private float _timer;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private Coroutine _warningCoroutine;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
        {
            _originalColor = _sr.color;
        }

        // Inicia o timer pronto para disparar no primeiro intervalo completo
        _timer = explosionInterval;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;

        // Inicia o aviso visual quando falta exatamente `warningDuration` para a explosão
        if (_warningCoroutine == null && _timer <= warningDuration && _timer > 0f)
        {
            _warningCoroutine = StartCoroutine(WarningFlash());
        }

        if (_timer <= 0f)
        {
            _timer = explosionInterval;
            _warningCoroutine = null;
            TriggerExplosion();
        }
    }

    /// <summary>
    /// Coroutine que pisca o SpriteRenderer do segmento durante o período de aviso,
    /// acelerando progressivamente para indicar que a explosão está iminente.
    /// </summary>
    private IEnumerator WarningFlash()
    {
        bool flashOn = false;
        float elapsed = 0f;
        float currentFlashSpeed = flashSpeed;

        while (elapsed < warningDuration)
        {
            flashOn = !flashOn;

            if (_sr != null)
            {
                _sr.color = flashOn ? warningColor : _originalColor;
            }

            // Acelera o piscar conforme a explosão se aproxima
            float progress = elapsed / warningDuration;
            currentFlashSpeed = Mathf.Lerp(flashSpeed, flashSpeed * 0.3f, progress);

            yield return new WaitForSeconds(currentFlashSpeed);
            elapsed += currentFlashSpeed;
        }

        // Garante que a cor original seja restaurada antes da explosão
        if (_sr != null)
        {
            _sr.color = _originalColor;
        }
    }

    /// <summary>
    /// Instancia o efeito visual e aplica dano em área a todos os inimigos
    /// dentro do raio configurado.
    /// </summary>
    private void TriggerExplosion()
    {
        Vector3 origin = transform.position;
        float currentRadius = EffectiveExplosionRadius;
        float currentDamage = EffectiveExplosionDamage;

        // Efeito visual (a escala máxima é sempre o dobro do raio de explosão)
        if (explosionEffectPrefab != null)
        {
            ExplosionEffect effect = Instantiate(explosionEffectPrefab, origin, Quaternion.identity);
            if (effect != null)
            {
                effect.Initialize(currentRadius * 2f);
            }
        }

        // Dano em área — atinge apenas inimigos (EnemyHealth), não segmentos aliados
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, currentRadius);
        if (hits == null) return;

        HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealth enemy = hits[i].GetComponent<EnemyHealth>();
            if (enemy == null)
            {
                enemy = hits[i].GetComponentInParent<EnemyHealth>();
            }

            if (enemy != null && !enemy.IsDead && damagedEnemies.Add(enemy))
            {
                enemy.TakeDamage(currentDamage, "bomber");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showRangeGizmo || EffectiveExplosionRadius <= 0f) return;

        // Laranja para diferenciar do raio vermelho do EnemyKamikaze
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, EffectiveExplosionRadius);
    }
}
