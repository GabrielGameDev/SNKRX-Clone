using UnityEngine;

/// <summary>
/// Representa um segmento individual do corpo da cobra (estilo SNKRX).
/// Cada segmento segue a trilha da cabeça e pode ser estendido futuramente
/// para conter mecânicas de ataque, vida ou atributos de classes/heróis.
/// </summary>
[DisallowMultipleComponent]
public class SnakeSegment : MonoBehaviour
{
    [Header("Segment Info")]
    [SerializeField] private int segmentIndex;

    private Rigidbody2D rb2d;
    
    public int SegmentIndex => segmentIndex;
    public SnakeMovement SnakeOwner { get; private set; }

    /// <summary>
    /// Inicializa o segmento com seu índice e referência ao SnakeMovement principal.
    /// </summary>
    public virtual void Initialize(int index, SnakeMovement owner)
    {
        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        segmentIndex = index;
        SnakeOwner = owner;
        gameObject.name = $"Segment_{index}";

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = -index;
        }
    }

    /// <summary>
    /// Atualiza o índice do segmento (ex: caso algum nó anterior seja removido).
    /// </summary>
    public virtual void SetIndex(int newIndex)
    {
        segmentIndex = newIndex;
        gameObject.name = $"Segment_{newIndex}";

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = -newIndex;
        }
    }

    /// <summary>
    /// Atualiza a posição e rotação calculadas pelo SnakeMovement.
    /// </summary>
    public virtual void UpdatePositionAndRotation(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (rb2d == null)
        {
            transform.SetPositionAndRotation(targetPosition, targetRotation);
            return;
        }

        if (SnakeOwner != null && SnakeOwner.IsWrapDiscontinuity(rb2d.position, targetPosition))
        {
            // O segmento já atravessou o limite no histórico da cobra. Teleporta
            // o Rigidbody2D para evitar que a interpolação o desenhe cruzando a arena.
            rb2d.position = targetPosition;
            rb2d.rotation = targetRotation.eulerAngles.z;
            return;
        }

        rb2d.MovePosition(targetPosition);
        rb2d.MoveRotation(targetRotation.eulerAngles.z);
    }

    /// <summary>
    /// Aplica uma cor customizada ao segmento e atualiza os componentes visuais e de gameplay vinculados.
    /// </summary>
    public virtual void SetSegmentColor(Color color)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
        }

        SegmentHealth health = GetComponent<SegmentHealth>();
        if (health != null)
        {
            health.SetBaseColor(color);
        }

        SegmentBomber bomber = GetComponent<SegmentBomber>();
        if (bomber != null)
        {
            bomber.SetBaseColor(color);
        }
    }
}
