using UnityEngine;

/// <summary>
/// Componente anexado a um segmento da cobra (SnakeSegment) que dispara
/// projéteis automaticamente em direção ao inimigo mais próximo.
/// Não persegue o inimigo após o disparo: define a direção inicial usando Vector2.up.
/// </summary>
[DisallowMultipleComponent]
public class SegmentShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    [Tooltip("Prefab do projétil que será instanciado.")]
    [SerializeField] private Projectile projectilePrefab;

    [Tooltip("Intervalo de tempo em segundos entre cada disparo.")]
    [SerializeField] private float fireInterval = 1f;

    [Tooltip("Alcance máximo para detectar e mirar em um inimigo (defina 0 ou menor para alcance ilimitado).")]
    [SerializeField] private float attackRange = 12f;

    [Tooltip("Ponto de onde o projétil será gerado. Se nulo, utiliza a posição do próprio segmento.")]
    [SerializeField] private Transform firePoint;

    [Header("Debug")]
    [Tooltip("Exibe o círculo de alcance no editor ao selecionar o objeto.")]
    [SerializeField] private bool showRangeGizmo = true;

    private float fireTimer;

    public float FireInterval { get => fireInterval; set => fireInterval = Mathf.Max(0.05f, value); }
    public float AttackRange { get => attackRange; set => attackRange = value; }
    public float EffectiveAttackRange => attackRange * (UpgradeManager.Instance != null ? UpgradeManager.Instance.RangeMultiplier : 1f);
    public float BaseProjectileDamage => projectilePrefab != null ? projectilePrefab.Damage : 0f;

    private void Awake()
    {
        if (firePoint == null)
        {
            firePoint = transform;
        }

        // Inicia pronto para atirar assim que avistar um alvo
        fireTimer = 0f;
    }

    private void Update()
    {
        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            TryShootNearestEnemy();
        }
    }

    /// <summary>
    /// Procura o inimigo mais próximo. Se encontrar um dentro do alcance,
    /// calcula o ângulo para alinhar o Vector2.up na direção dele e instancia o projétil.
    /// </summary>
    private void TryShootNearestEnemy()
    {
        if (projectilePrefab == null) return;

        EnemyHealth target = FindNearestEnemy();
        if (target == null)
        {
            // Mantém pronto para disparar assim que um inimigo entrar no alcance
            fireTimer = 0f;
            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Vector2 direction = ((Vector2)target.transform.position - (Vector2)spawnPosition).normalized;

        if (direction != Vector2.zero)
        {
            // Calcula a rotação necessária no plano 2D para que o Vector2.up aponte para o alvo
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            Projectile proj = Instantiate(projectilePrefab, spawnPosition, rotation);
            SfxPlayer.PlayGunfire();
            if (proj != null && UpgradeManager.Instance != null)
            {
                proj.Damage *= UpgradeManager.Instance.GlobalDamageMultiplier;
            }

            // Reinicia o tempo para o próximo disparo
            fireTimer = fireInterval;
        }
    }

    /// <summary>
    /// Retorna o EnemyHealth ativo mais próximo do segmento que esteja dentro do alcance configurado.
    /// </summary>
    private EnemyHealth FindNearestEnemy()
    {
        var enemies = EnemyHealth.AllEnemies;
        if (enemies == null || enemies.Count == 0) return null;

        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        EnemyHealth nearest = null;
        float currentRange = EffectiveAttackRange;
        float shortestDistanceSqr = (currentRange > 0f) ? (currentRange * currentRange) : float.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null || enemy.IsDead) continue;

            float distanceSqr = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
            if (distanceSqr < shortestDistanceSqr)
            {
                shortestDistanceSqr = distanceSqr;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showRangeGizmo || attackRange <= 0f) return;

        Vector3 center = firePoint != null ? firePoint.position : transform.position;
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(center, attackRange);
    }
}
