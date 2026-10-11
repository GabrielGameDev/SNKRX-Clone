using UnityEngine;
using System.Collections;

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
    [SerializeField] private AudioClip upgradeCardSelectClip;
    [SerializeField] private AudioClip xpPickupClip;
    [SerializeField] private AudioClip gameOverClip;

    [Header("Combat Time Effects")]
    [SerializeField, Min(0f)] private float damageFreezeDuration = 0.06f;
    [SerializeField, Range(0f, 1f)] private float damageFreezeScale = 0f;
    [SerializeField, Min(0f)] private float segmentDeathSlowDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float segmentDeathSlowScale = 0.25f;

    [Header("Damage Camera Shake")]
    [SerializeField, Min(0f)] private float damageShakeDuration = 0.16f;
    [SerializeField, Min(0f)] private float damageShakeMagnitude = 0.12f;

    private Coroutine timeEffectCoroutine;
    private float freezeEndTime;
    private float slowEndTime;
    private float timeScaleBeforeEffect;
    private Coroutine cameraShakeCoroutine;
    private Transform shakenCamera;
    private Vector3 cameraRestLocalPosition;

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
    public static void PlayUpgradeCardSelect() => Instance?.Play(Instance.upgradeCardSelectClip);
    public static void PlayXpPickup() => Instance?.Play(Instance.xpPickupClip);
    public static void PlayGameOver() => Instance?.Play(Instance.gameOverClip);
    public static void PlayDamageFreeze()
    {
        if (Instance == null) return;
        Instance.StartDamageFreeze();
        Instance.StartDamageCameraShake();
    }
    public static void PlaySegmentDeathSlow() => Instance?.StartSegmentDeathSlow();

    private void StartDamageFreeze() => StartTimeEffect(damageFreezeScale, damageFreezeDuration, true);
    private void StartSegmentDeathSlow() => StartTimeEffect(segmentDeathSlowScale, segmentDeathSlowDuration, false);

    private void StartTimeEffect(float scale, float duration, bool isFreeze)
    {
        if (duration <= 0f) return;

        float endTime = Time.unscaledTime + duration;
        if (isFreeze)
            freezeEndTime = Mathf.Max(freezeEndTime, endTime);
        else
            slowEndTime = Mathf.Max(slowEndTime, endTime);

        if (timeEffectCoroutine == null)
        {
            timeScaleBeforeEffect = Time.timeScale;
            timeEffectCoroutine = StartCoroutine(TimeEffectRoutine());
        }
    }

    private IEnumerator TimeEffectRoutine()
    {
        while (Time.unscaledTime < freezeEndTime || Time.unscaledTime < slowEndTime)
        {
            Time.timeScale = Time.unscaledTime < freezeEndTime
                ? damageFreezeScale
                : segmentDeathSlowScale;
            yield return null;
        }

        Time.timeScale = timeScaleBeforeEffect;
        freezeEndTime = 0f;
        slowEndTime = 0f;
        timeEffectCoroutine = null;
    }

    private void StartDamageCameraShake()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || damageShakeDuration <= 0f || damageShakeMagnitude <= 0f) return;

        if (cameraShakeCoroutine != null)
        {
            StopCoroutine(cameraShakeCoroutine);
            if (shakenCamera != null) shakenCamera.localPosition = cameraRestLocalPosition;
        }

        shakenCamera = mainCamera.transform;
        cameraRestLocalPosition = shakenCamera.localPosition;
        cameraShakeCoroutine = StartCoroutine(CameraShakeRoutine());
    }

    private IEnumerator CameraShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < damageShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 offset = Random.insideUnitCircle * damageShakeMagnitude;
            shakenCamera.localPosition = cameraRestLocalPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        if (shakenCamera != null) shakenCamera.localPosition = cameraRestLocalPosition;
        cameraShakeCoroutine = null;
        shakenCamera = null;
    }

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
