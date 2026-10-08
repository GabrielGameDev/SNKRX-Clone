using UnityEngine;

/// <summary>
/// Inimigo Tanque: fica completamente parado, possui maior tamanho e pontos de vida,
/// e dispara projéteis periodicamente na direção do jogador.
/// </summary>
[DisallowMultipleComponent]
public class EnemyTank : MonoBehaviour
{
    [Header("Shooting Settings")]
    [Tooltip("Prefab do projétil que o Tanque dispara.")]
    [SerializeField] private EnemyProjectile projectilePrefab;

    [Tooltip("Intervalo entre cada tiro em segundos.")]
    [SerializeField] private float fireInterval = 2.4f;

    [Tooltip("Alcance máximo para detectar e atirar na cobra (0 para ilimitado).")]
    [SerializeField] private float attackRange = 16f;

    [Tooltip("Ponto de saída do tiro. Se nulo, utiliza a posição do próprio Tanque.")]
    [SerializeField] private Transform firePoint;

    [Header("Combat Settings")]
    [Tooltip("Dano de contato aplicado caso a cobra encoste diretamente no Tanque.")]
    [SerializeField] private float contactDamage = 1.5f;

    private float fireTimer;

    private void Awake()
    {
        if (firePoint == null)
        {
            firePoint = transform;
        }

        // Começa com um pequeno delay inicial para não atirar no exato milissegundo em que surge
        fireTimer = fireInterval * 0.5f;
    }

    private void Update()
    {
        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            TryShootAtPlayer();
        }
    }

    private void TryShootAtPlayer()
    {
        if (projectilePrefab == null || SnakeMovement.Instance == null) return;

        Vector3 targetPos = SnakeMovement.Instance.HeadPosition;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        float distance = Vector2.Distance(spawnPos, targetPos);
        if (attackRange > 0f && distance > attackRange)
        {
            return;
        }

        Vector2 direction = ((Vector2)targetPos - (Vector2)spawnPos).normalized;
        if (direction != Vector2.zero)
        {
            // Calcula rotação 2D com Vector2.up apontando para a direção do alvo
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            Instantiate(projectilePrefab, spawnPos, rotation);
            fireTimer = fireInterval;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryDealDamage(collision);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDealDamage(collision);
    }

    private void TryDealDamage(Collider2D collision)
    {
        SegmentHealth segment = collision.GetComponent<SegmentHealth>();
        if (segment == null)
        {
            segment = collision.GetComponentInParent<SegmentHealth>();
        }

        if (segment != null && !segment.IsDead)
        {
            segment.TakeDamage(contactDamage);
        }
    }
}
