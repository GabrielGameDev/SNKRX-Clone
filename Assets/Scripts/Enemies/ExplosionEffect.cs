using System.Collections;
using UnityEngine;

/// <summary>
/// Efeito visual procedural da explosão: expande um elemento visual e faz fade-out rápido antes de se destruir.
/// </summary>
[DisallowMultipleComponent]
public class ExplosionEffect : MonoBehaviour
{
    [Tooltip("Duração total da animação de explosão em segundos.")]
    [SerializeField] private float duration = 0.35f;

    [Tooltip("Escala máxima atingida pela explosão.")]
    [SerializeField] private float maxScale = 3f;

    [Tooltip("SpriteRenderer do efeito.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    public float MaxScale { get => maxScale; set => maxScale = value; }
    private float targetDiameter;
    private bool useTargetDiameter;

    /// <summary>
    /// Define o diâmetro máximo visual em unidades do mundo, para coincidir com a área de efeito.
    /// </summary>
    public void Initialize(float diameter)
    {
        targetDiameter = diameter;
        useTargetDiameter = true;
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        StartCoroutine(AnimateExplosion());
    }

    private IEnumerator AnimateExplosion()
    {
        Vector3 finalScale = Vector3.one * maxScale;
        if (useTargetDiameter && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
                finalScale = new Vector3(targetDiameter / spriteSize.x, targetDiameter / spriteSize.y, 1f);
        }

        Vector3 initialScale = finalScale * 0.1f;
        transform.localScale = initialScale;

        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.yellow;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Expansão exponencial rápida
            float scaleProgress = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.localScale = Vector3.Lerp(initialScale, finalScale, scaleProgress);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
