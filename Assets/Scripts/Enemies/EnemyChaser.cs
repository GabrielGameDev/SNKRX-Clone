using UnityEngine;

/// <summary>
/// Inimigo que se move mais rápido perseguindo o jogador ativamente.
/// Causa dano de contato ao alcançar a cabeça ou qualquer segmento da cobra.
/// </summary>
[DisallowMultipleComponent]
public class EnemyChaser : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Velocidade de perseguição do inimigo.")]
    [SerializeField] private float speed = 3.6f;

    [Tooltip("Se verdadeiro, persegue o segmento mais próximo; se falso, persegue sempre a cabeça.")]
    [SerializeField] private bool targetNearestSegment = false;

    [Tooltip("Margem das bordas da tela para não sair da arena.")]
    [SerializeField] private float edgeMargin = 0.5f;

    [Header("Combat Settings")]
    [Tooltip("Dano de contato aplicado a segmentos da cobra.")]
    [SerializeField] private float contactDamage = 1f;

    private Rigidbody2D rb2d;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        ChaseTarget();
    }

    private void ChaseTarget()
    {
        if (SnakeMovement.Instance == null) return;

        Vector3 targetPos = targetNearestSegment
            ? SnakeMovement.Instance.GetNearestTargetPosition(transform.position)
            : SnakeMovement.Instance.HeadPosition;

        Vector2 direction = ((Vector2)targetPos - (Vector2)transform.position).normalized;
        direction = (direction + EnemySeparation.GetDirection(transform) * 1.5f).normalized;

        if (direction != Vector2.zero)
        {
            Vector2 nextPos = (Vector2)transform.position + (direction * (speed * Time.deltaTime));

            // Mantém o inimigo dentro dos limites da tela
            Bounds2D bounds = ArenaBounds.GetBounds(edgeMargin);
            nextPos = bounds.Clamp(nextPos);

            transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
            if (rb2d != null)
            {
                rb2d.position = nextPos;
            }
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
