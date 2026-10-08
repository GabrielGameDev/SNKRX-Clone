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
        Vector3 initialScale = Vector3.one * 0.2f;
        transform.localScale = initialScale;

        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.yellow;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Expansão exponencial rápida
            float scaleMultiplier = Mathf.Lerp(0.2f, maxScale, Mathf.Sin(t * Mathf.PI * 0.5f));
            transform.localScale = Vector3.one * scaleMultiplier;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
