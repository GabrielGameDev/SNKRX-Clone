using UnityEngine;

/// <summary>
/// Item coletável que concede XP ao jogador ao entrar em contato com ele.
/// O prefab precisa de um Collider2D marcado como Trigger.
/// </summary>
[DisallowMultipleComponent]
public class XpPickup : MonoBehaviour
{
    [Tooltip("XP concedido ao jogador ao coletar este item.")]
    [Min(1)]
    [SerializeField] private int experienceAmount = 1;

    private bool collected;

    public int ExperienceAmount => experienceAmount;

    /// <summary>Define o valor do item, usado pelo drop configurado no inimigo.</summary>
    public void Initialize(int amount)
    {
        experienceAmount = Mathf.Max(1, amount);
    }

    private void Update()
    {
        if (collected) return;
        if (UpgradeManager.Instance == null || UpgradeManager.Instance.MagnetRadius <= 0f) return;
        if (SnakeMovement.Instance == null) return;

        Vector3 headPos = SnakeMovement.Instance.HeadPosition;
        float distSqr = (headPos - transform.position).sqrMagnitude;
        float radius = UpgradeManager.Instance.MagnetRadius;

        if (distSqr <= radius * radius)
        {
            float speed = UpgradeManager.Instance.MagnetPullSpeed;
            float dist = Mathf.Sqrt(distSqr);
            float currentSpeed = Mathf.Max(speed, speed * (1f + (radius - dist) * 0.5f));
            transform.position = Vector3.MoveTowards(transform.position, headPos, currentSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        PlayerExperience playerExperience = other.GetComponentInParent<PlayerExperience>();

        // Permite que qualquer segmento da cobra colete o item, mesmo quando o
        // PlayerExperience está anexado à cabeça e os segmentos são objetos irmãos.
        if (playerExperience == null)
        {
            SnakeSegment segment = other.GetComponentInParent<SnakeSegment>();
            if (segment != null && segment.SnakeOwner != null)
            {
                playerExperience = segment.SnakeOwner.GetComponentInParent<PlayerExperience>();
            }
        }

        if (playerExperience == null) return;

        collected = true;
        SfxPlayer.PlayXpPickup();
        playerExperience.AddExperience(experienceAmount);
        Destroy(gameObject);
    }
}
