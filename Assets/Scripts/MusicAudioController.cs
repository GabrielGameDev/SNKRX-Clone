using UnityEngine;

/// <summary>Maintains scene music and applies audio effects for game screens.</summary>
[RequireComponent(typeof(AudioSource))]
public sealed class MusicAudioController : MonoBehaviour
{
    private static MusicAudioController instance;

    [SerializeField, Min(10f)] private float menuCutoffFrequency = 500f;
    [SerializeField, Min(10f)] private float gameplayCutoffFrequency = 5000f;
    [SerializeField, Min(0f)] private float pitchVariation = 0.12f;
    [SerializeField, Min(0.01f)] private float pitchChangeInterval = 0.15f;

    private AudioSource musicSource;
    private AudioLowPassFilter lowPassFilter;
    private bool pitchEffectActive;
    private float nextPitchChangeTime;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        musicSource = GetComponent<AudioSource>();
        lowPassFilter = GetComponent<AudioLowPassFilter>();
        if (lowPassFilter == null) lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();

        SetMenuState();
    }

    private void Update()
    {
        if (!pitchEffectActive || Time.unscaledTime < nextPitchChangeTime) return;

        musicSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        nextPitchChangeTime = Time.unscaledTime + pitchChangeInterval;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void SetMenuState()
    {
        if (instance == null) return;
        instance.lowPassFilter.cutoffFrequency = instance.menuCutoffFrequency;
        instance.StopPitchEffect();
    }

    public static void SetGameplayState()
    {
        if (instance == null) return;
        instance.lowPassFilter.cutoffFrequency = instance.gameplayCutoffFrequency;
        instance.StopPitchEffect();
    }

    public static void SetPausedScreenState()
    {
        if (instance == null) return;
        instance.pitchEffectActive = true;
        instance.nextPitchChangeTime = 0f;
    }

    private void StopPitchEffect()
    {
        pitchEffectActive = false;
        musicSource.pitch = 1f;
    }
}
