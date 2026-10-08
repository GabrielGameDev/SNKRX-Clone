using UnityEngine;

/// <summary>
/// Projétil disparado por inimigos (como o Tanque) em direção à cobra.
/// Causa dano ao colidir com qualquer segmento da cobra (incluindo a cabeça).
/// </summary>
[DisallowMultipleComponent]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Velocidade do projétil inimigo.")]
    [SerializeField] private float speed = 7f;

    [Tooltip("Dano causado ao atingir um segmento da cobra.")]
    [SerializeField] private float damage = 1f;

    [Tooltip("Tempo de vida máximo em segundos.")]
    [SerializeField] private float lifetime = 5f;

    private float timer;

    public float Damage { get => damage; set => damage = value; }
    public float Speed { get => speed; set => speed = value; }

    public void Initialize(float customDamage, float customSpeed)
    {
        damage = customDamage;
        speed = customSpeed;
    }

    private void Update()
    {
        // Move-se para frente na direção do seu Vector2.up local
        transform.Translate(Vector2.up * (speed * Time.deltaTime), Space.Self);

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        SegmentHealth segment = collision.GetComponent<SegmentHealth>();
        if (segment == null)
        {
            segment = collision.GetComponentInParent<SegmentHealth>();
        }

        if (segment != null && !segment.IsDead)
        {
            segment.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
