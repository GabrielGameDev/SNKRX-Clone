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
    
    public int SegmentIndex => segmentIndex;
    public SnakeMovement SnakeOwner { get; private set; }

    /// <summary>
    /// Inicializa o segmento com seu índice e referência ao SnakeMovement principal.
    /// </summary>
    public virtual void Initialize(int index, SnakeMovement owner)
    {
        segmentIndex = index;
        SnakeOwner = owner;
        gameObject.name = $"Segment_{index}";
    }

    /// <summary>
    /// Atualiza o índice do segmento (ex: caso algum nó anterior seja removido).
    /// </summary>
    public virtual void SetIndex(int newIndex)
    {
        segmentIndex = newIndex;
        gameObject.name = $"Segment_{newIndex}";
    }

    /// <summary>
    /// Atualiza a posição e rotação calculadas pelo SnakeMovement.
    /// </summary>
    public virtual void UpdatePositionAndRotation(Vector3 targetPosition, Quaternion targetRotation)
    {
        transform.SetPositionAndRotation(targetPosition, targetRotation);
    }
}
