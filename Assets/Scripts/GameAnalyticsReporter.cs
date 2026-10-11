using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

/// <summary>Initializes UGS Analytics and buffers gameplay events until it is ready.</summary>
public sealed class GameAnalyticsReporter : MonoBehaviour
{
    private static GameAnalyticsReporter instance;
    private readonly Queue<PendingEvent> pendingEvents = new Queue<PendingEvent>();
    private bool initialized;
    private bool flushAfterInitialization;
    private string currentRunId;

    private sealed class PendingEvent
    {
        public string Name;
        public Dictionary<string, object> Parameters;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject(nameof(GameAnalyticsReporter));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameAnalyticsReporter>();
    }

    private async void Awake()
    {

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SetAnalyticsConsent(true);
        DontDestroyOnLoad(gameObject);
        try
        {
            await UnityServices.InitializeAsync();
            initialized = true;
            LogDebug("Unity Gaming Services initialized.");
            ConsentState consent = EndUserConsent.GetConsentState();
            LogDebug($"Analytics consent status: {consent.AnalyticsIntent}.");
            if (consent.AnalyticsIntent != ConsentStatus.Granted)
            {
                Debug.LogWarning($"[Analytics] Analytics consent is {consent.AnalyticsIntent}; the SDK will ignore custom events until AnalyticsIntent is Granted.");
            }
            while (pendingEvents.Count > 0)
            {
                PendingEvent item = pendingEvents.Dequeue();
                Send(item.Name, item.Parameters);
            }

            if (flushAfterInitialization)
            {
                flushAfterInitialization = false;
                Flush();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Analytics] Unity Gaming Services initialization failed: {exception.Message}");
        }
    }

    public void SetAnalyticsConsent(bool accepted)
    {
        var consent = EndUserConsent.GetConsentState();
        consent.AnalyticsIntent = accepted
            ? ConsentStatus.Granted
            : ConsentStatus.Denied;

        EndUserConsent.SetConsentState(consent);
    }

    public static void Record(string eventName, Dictionary<string, object> parameters)
    {
        if (instance == null) Bootstrap();
        if (instance == null) return;

        if (!string.IsNullOrEmpty(instance.currentRunId) && !parameters.ContainsKey("run_id"))
            parameters["run_id"] = instance.currentRunId;

        var item = new PendingEvent { Name = eventName, Parameters = parameters };
        if (!instance.initialized)
        {
            instance.pendingEvents.Enqueue(item);
            LogDebug($"Event '{eventName}' queued until Unity Gaming Services is ready.");
            return;
        }

        instance.Send(item.Name, item.Parameters);
    }

    public static void SetRunId(string runId)
    {
        if (instance == null) Bootstrap();
        if (instance != null) instance.currentRunId = runId;
    }

    public static void Flush()
    {
        if (instance == null) Bootstrap();
        if (instance == null) return;
        if (!instance.initialized)
        {
            instance.flushAfterInitialization = true;
            LogDebug("Flush queued until Unity Gaming Services is ready.");
            return;
        }

        try
        {
            AnalyticsService.Instance.Flush();
            LogDebug("Flush requested from the Analytics SDK.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Analytics] Could not flush events: {exception.Message}");
        }
    }

    private void Send(string eventName, Dictionary<string, object> parameters)
    {
        ConsentState consent = EndUserConsent.GetConsentState();
        if (consent.AnalyticsIntent != ConsentStatus.Granted)
            return;

        try
        {
            var customEvent = new CustomEvent(eventName);
            foreach (KeyValuePair<string, object> parameter in parameters)
                customEvent.Add(parameter.Key, parameter.Value);

            AnalyticsService.Instance.RecordEvent(customEvent);
            LogDebug($"Event '{eventName}' recorded by the Analytics SDK.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Analytics] Could not record '{eventName}': {exception.Message}");
        }
    }

    private static void LogDebug(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Analytics] {message}");
#endif
    }
}
