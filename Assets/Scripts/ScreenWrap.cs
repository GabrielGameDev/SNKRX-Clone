using UnityEngine;

/// <summary>
/// Componente genérico e reutilizável para aplicar o comportamento estilo Pac-Man (screen wrap)
/// em qualquer GameObject (como projéteis, inimigos ou itens do cenário).
/// Faz com que o objeto reapareça do lado oposto ao cruzar os limites da tela ou arena.
/// </summary>
[DisallowMultipleComponent]
public class ScreenWrap : MonoBehaviour
{
    [Header("Wrap Settings")]
    [Tooltip("Calcula os limites automaticamente a partir da visão da câmera ortográfica principal.")]
    [SerializeField] private bool useCameraBounds = true;

    [Tooltip("Câmera de referência para os limites (se vazia, utiliza a Camera.main).")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Dimensões manuais da arena (Largura X, Altura Y) caso use limites fixos.")]
    [SerializeField] private Vector2 customBoundsSize = new Vector2(35.55f, 20f);

    [Tooltip("Centro da arena de jogo.")]
    [SerializeField] private Vector2 boundsCenter = Vector2.zero;

    [Tooltip("Habilita o wrap horizontal (Esquerda <-> Direita).")]
    [SerializeField] private bool wrapHorizontal = true;

    [Tooltip("Habilita o wrap vertical (Cima <-> Baixo).")]
    [SerializeField] private bool wrapVertical = true;

    [Tooltip("Margem além da borda antes de teleportar.")]
    [SerializeField] private float wrapPadding = 0.5f;

    private Rigidbody2D rb2d;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        Vector2 halfSize;
        Vector2 center;

        if (useCameraBounds)
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null && targetCamera.orthographic)
            {
                float h = targetCamera.orthographicSize;
                float w = h * targetCamera.aspect;
                halfSize = new Vector2(w + wrapPadding, h + wrapPadding);
                center = (Vector2)targetCamera.transform.position + boundsCenter;
            }
            else
            {
                halfSize = (customBoundsSize * 0.5f) + new Vector2(wrapPadding, wrapPadding);
                center = boundsCenter;
            }
        }
        else
        {
            halfSize = (customBoundsSize * 0.5f) + new Vector2(wrapPadding, wrapPadding);
            center = boundsCenter;
        }

        Vector3 pos = transform.position;
        float width = halfSize.x * 2f;
        float height = halfSize.y * 2f;

        if (wrapHorizontal && width > 0.001f)
        {
            float minX = center.x - halfSize.x;
            float maxX = center.x + halfSize.x;
            while (pos.x > maxX) pos.x -= width;
            while (pos.x < minX) pos.x += width;
        }

        if (wrapVertical && height > 0.001f)
        {
            float minY = center.y - halfSize.y;
            float maxY = center.y + halfSize.y;
            while (pos.y > maxY) pos.y -= height;
            while (pos.y < minY) pos.y += height;
        }

        if (pos != transform.position)
        {
            transform.position = pos;
            if (rb2d != null)
            {
                rb2d.position = pos;
            }
        }
    }
}
