using UnityEngine;

/// <summary>
/// Controla o comportamento do projétil disparado pelo jogador.
/// Move-se continuamente na direção do seu eixo local 'up' (Vector2.up),
/// colide com inimigos causando dano e se autodestrói.
/// </summary>
[DisallowMultipleComponent]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Velocidade constante de avanço do projétil.")]
    [SerializeField] private float speed = 14f;

    [Tooltip("Dano causado ao atingir um inimigo.")]
    [SerializeField] private float damage = 1f;

    [Tooltip("Tempo de vida máximo em segundos antes do projétil se autodestruir.")]
    [SerializeField] private float lifetime = 3f;

    private float timer;

    public float Damage { get => damage; set => damage = value; }
    public float Speed { get => speed; set => speed = value; }

    /// <summary>
    /// Permite inicializar parâmetros dinamicamente (ex: bônus de dano ou velocidade).
    /// </summary>
    public void Initialize(float customDamage, float customSpeed)
    {
        damage = customDamage;
        speed = customSpeed;
    }

    private void Update()
    {
        // Move-se para frente na direção do seu Vector2.up local
        transform.Translate(Vector2.up * (speed * Time.deltaTime), Space.Self);

        // Temporizador de destruição caso não atinja nada
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Tenta encontrar o componente EnemyHealth no objeto atingido ou em seus pais
        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();
        if (enemy == null)
        {
            enemy = collision.GetComponentInParent<EnemyHealth>();
        }

        if (enemy != null && !enemy.IsDead)
        {
            enemy.TakeDamage(damage, "shooter");
            Destroy(gameObject);
        }
    }
}
