using UnityEngine;

/// <summary>
/// Reproduz os efeitos sonoros compartilhados do jogo.
/// </summary>
public sealed class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; }

    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip explosionClip;
    [SerializeField] private AudioClip gunfireClip;
    [SerializeField] private AudioClip upgradeClip;
    [SerializeField] private AudioClip xpPickupClip;
    [SerializeField] private AudioClip gameOverClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void PlayHit() => Instance?.Play(Instance.hitClip);
    public static void PlayExplosion() => Instance?.Play(Instance.explosionClip);
    public static void PlayGunfire() => Instance?.Play(Instance.gunfireClip);
    public static void PlayUpgrade() => Instance?.Play(Instance.upgradeClip);
    public static void PlayXpPickup() => Instance?.Play(Instance.xpPickupClip);
    public static void PlayGameOver() => Instance?.Play(Instance.gameOverClip);

    private void Play(AudioClip clip)
    {
        if (clip == null) return;

        GameObject soundObject = new GameObject($"SFX - {clip.name}");
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.PlayOneShot(clip);
        Destroy(soundObject, clip.length);
    }
}
