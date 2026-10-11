using System.Collections;
using UnityEngine;

/// <summary>
/// Inimigo Kamikaze: persegue o jogador, para ao se aproximar,
/// inicia uma contagem regressiva piscando com aviso visual e detona em área.
/// </summary>
[DisallowMultipleComponent]
public class EnemyKamikaze : MonoBehaviour
{
    private enum State { Chasing, Priming, Exploded }

    [Header("Movement Settings")]
    [Tooltip("Velocidade de perseguição até o jogador.")]
    [SerializeField] private float speed = 3.2f;

    [Tooltip("Distância da cobra em que o inimigo para e inicia a contagem da bomba.")]
    [SerializeField] private float stopDistance = 2.4f;

    [Tooltip("Margem das bordas da tela.")]
    [SerializeField] private float edgeMargin = 0.5f;

    [Header("Explosion Settings")]
    [Tooltip("Tempo em segundos que o inimigo espera parado antes de explodir.")]
    [SerializeField] private float fuseTime = 1.2f;

    [Tooltip("Raio da área de dano da explosão.")]
    [SerializeField] private float explosionRadius = 2.8f;

    [Tooltip("Dano causado aos segmentos da cobra pegos na explosão.")]
    [SerializeField] private float explosionDamage = 2f;

    [Header("Contact Settings")]
    [Tooltip("Dano aplicado aos segmentos da cobra ao encostar no kamikaze.")]
    [SerializeField] private float contactDamage = 1f;

    [Tooltip("Prefab opcional do efeito visual da explosão.")]
    [SerializeField] private ExplosionEffect explosionEffectPrefab;

    [Header("Visual Feedback")]
    [Tooltip("SpriteRenderer para efeito de piscar durante o priming.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Cor do piscar de alerta.")]
    [SerializeField] private Color warningFlashColor = Color.red;

    private State currentState = State.Chasing;
    private Color originalColor = Color.yellow;
    private Rigidbody2D rb2d;
    private Coroutine primingCoroutine;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();

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
        if (currentState == State.Chasing)
        {
            UpdateChasing();
        }
    }

    private void UpdateChasing()
    {
        if (SnakeMovement.Instance == null) return;

        Vector3 targetPos = SnakeMovement.Instance.HeadPosition;
        float distance = Vector2.Distance(transform.position, targetPos);

        // Se chegou perto o suficiente da cobra, para e começa o priming
        if (distance <= stopDistance)
        {
            StartPriming();
            return;
        }

        Vector2 direction = ((Vector2)targetPos - (Vector2)transform.position).normalized;
        if (direction != Vector2.zero)
        {
            Vector2 nextPos = (Vector2)transform.position + (direction * (speed * Time.deltaTime));

            Bounds2D bounds = ArenaBounds.GetBounds(edgeMargin);
            nextPos = bounds.Clamp(nextPos);

            transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
            if (rb2d != null)
            {
                rb2d.position = nextPos;
            }
        }
    }

    private void StartPriming()
    {
        currentState = State.Priming;
        primingCoroutine = StartCoroutine(PrimingRoutine());
    }

    private IEnumerator PrimingRoutine()
    {
        float timer = 0f;
        float flashInterval = 0.25f;
        bool isFlashing = false;

        while (timer < fuseTime)
        {
            timer += flashInterval;

            // Acelera o piscar progressivamente conforme chega perto de explodir
            float progress = timer / fuseTime;
            flashInterval = Mathf.Lerp(0.2f, 0.05f, progress);

            isFlashing = !isFlashing;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isFlashing ? warningFlashColor : originalColor;
            }

            yield return new WaitForSeconds(flashInterval);
        }

        Explode();
    }

    private void Explode()
    {
        if (currentState == State.Exploded) return;
        currentState = State.Exploded;

        // Efeito visual
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // Dano em área
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        if (hits != null)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                SegmentHealth segment = hits[i].GetComponent<SegmentHealth>();
                if (segment == null)
                {
                    segment = hits[i].GetComponentInParent<SegmentHealth>();
                }

                if (segment != null && !segment.IsDead)
                {
                    segment.TakeDamage(explosionDamage);
                }
            }
        }

        // Destrói o inimigo
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDealContactDamage(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryDealContactDamage(collision);
    }

    private void TryDealContactDamage(Collider2D collision)
    {
        if (currentState == State.Exploded) return;

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

    private void OnDestroy()
    {
        if (primingCoroutine != null)
        {
            StopCoroutine(primingCoroutine);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
