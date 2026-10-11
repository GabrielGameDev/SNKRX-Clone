using UnityEngine;

/// <summary>
/// Comportamento do inimigo básico: move-se lentamente pelo cenário,
/// quicando suavemente ao atingir os limites da tela (sem ultrapassá-los),
/// e causa dano por contato caso encoste na cobra ou em seus segmentos.
/// </summary>
[DisallowMultipleComponent]
public class EnemyWanderer : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Velocidade lenta de movimentação.")]
    [SerializeField] private float speed = 1.8f;

    [Tooltip("Margem das bordas da tela para colisão/rebote.")]
    [SerializeField] private float edgeMargin = 0.6f;

    [Tooltip("Intervalo para sortear pequenas variações na direção da caminhada.")]
    [SerializeField] private float directionChangeInterval = 3.5f;

    [Header("Combat Settings")]
    [Tooltip("Dano de contato aplicado a segmentos da cobra.")]
    [SerializeField] private float contactDamage = 1f;

    private Vector2 currentDirection;
    private float directionTimer;
    private Rigidbody2D rb2d;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        PickRandomDirection();
        directionTimer = Random.Range(directionChangeInterval * 0.5f, directionChangeInterval);
    }

    private void Update()
    {
        MoveAndBounceWithinBounds();
        HandleRandomDirectionChange();
    }

    private void PickRandomDirection()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        currentDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
        if (currentDirection == Vector2.zero)
        {
            currentDirection = Vector2.right;
        }
    }

    private void HandleRandomDirectionChange()
    {
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0f)
        {
            // Aplica um leve desvio no ângulo atual
            float angleDelta = Random.Range(-45f, 45f) * Mathf.Deg2Rad;
            float currentAngle = Mathf.Atan2(currentDirection.y, currentDirection.x);
            float newAngle = currentAngle + angleDelta;
            currentDirection = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)).normalized;

            directionTimer = Random.Range(directionChangeInterval * 0.8f, directionChangeInterval * 1.4f);
        }
    }

    private void MoveAndBounceWithinBounds()
    {
        Vector2 separation = EnemySeparation.GetDirection(transform);
        Vector2 moveDirection = (currentDirection + separation * 1.5f).normalized;
        if (moveDirection == Vector2.zero) moveDirection = currentDirection;
        Vector2 nextPos = (Vector2)transform.position + (moveDirection * (speed * Time.deltaTime));
        Bounds2D bounds = ArenaBounds.GetBounds(edgeMargin);

        // Verifica colisões com os limites horizontal e vertical
        if (nextPos.x <= bounds.minX)
        {
            nextPos.x = bounds.minX;
            currentDirection.x = Mathf.Abs(currentDirection.x); // rebate para a direita
        }
        else if (nextPos.x >= bounds.maxX)
        {
            nextPos.x = bounds.maxX;
            currentDirection.x = -Mathf.Abs(currentDirection.x); // rebate para a esquerda
        }

        if (nextPos.y <= bounds.minY)
        {
            nextPos.y = bounds.minY;
            currentDirection.y = Mathf.Abs(currentDirection.y); // rebate para cima
        }
        else if (nextPos.y >= bounds.maxY)
        {
            nextPos.y = bounds.maxY;
            currentDirection.y = -Mathf.Abs(currentDirection.y); // rebate para baixo
        }

        transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
        if (rb2d != null)
        {
            rb2d.position = nextPos;
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
