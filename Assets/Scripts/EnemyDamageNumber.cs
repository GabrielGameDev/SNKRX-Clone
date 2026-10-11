using TMPro;
using UnityEngine;

/// <summary>Small world-space damage feedback that rises and fades after a hit.</summary>
public class EnemyDamageNumber : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float lifetime = 0.75f;
    [SerializeField] private float riseSpeed = 1.6f;

    private TextMeshPro text;
    private float elapsed;
    private Color startColor;
    private Vector3 startScale;

    public static void Spawn(Vector3 position, float damage)
    {
        if (damage <= 0f) return;

        var go = new GameObject("Damage Number");
        go.transform.position = position + new Vector3(Random.Range(-0.18f, 0.18f), 0.35f, 0f);
        var number = go.AddComponent<EnemyDamageNumber>();
        number.Initialize(damage);
    }

    private void Initialize(float damage)
    {
        text = gameObject.AddComponent<TextMeshPro>();
        text.text = damage.ToString("0.#");
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 50f;
        text.font = TMP_Settings.defaultFontAsset;
        text.color = new Color(1f, 0.92f, 0.48f, 1f);
        text.textWrappingMode = TextWrappingModes.NoWrap;

        var renderer = GetComponent<MeshRenderer>();
        renderer.sortingOrder = 20;

        Camera targetCamera = Camera.main;
        if (targetCamera != null)
            transform.rotation = Quaternion.LookRotation(targetCamera.transform.forward, targetCamera.transform.up);

        startScale = Vector3.one * 0.1f;
        transform.localScale = startScale * 0.75f;
        startColor = text.color;
        text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
        text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.12f, 0.08f, 0.03f, 1f));
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / lifetime);
        transform.position += Vector3.up * (riseSpeed * Time.unscaledDeltaTime);

        float punch = 1f + Mathf.Sin(Mathf.Clamp01(progress * 5f) * Mathf.PI) * 0.22f;
        transform.localScale = startScale * punch;

        Color color = startColor;
        color.a = 1f - progress;
        text.color = color;

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}
