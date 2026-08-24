using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [SerializeField] private float interstitialMinInterval = 75f;
    [SerializeField] private int minRunsBetweenInterstitials = 2;

    private readonly List<IAdsProvider> providers = new();
    private IAdsProvider active;
    private float lastInterstitialRealtime = -9999f;
    private int runsSinceInterstitial;

    public string ActiveProviderName => active?.Name ?? "none";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        providers.Add(new YandexAdsProvider());
        providers.Add(new VkAdsProvider());
    }

    private void Start()
    {
        foreach (IAdsProvider provider in providers)
        {
            try
            {
                bool ready = false;
                provider.Initialize(ok => ready = ok);
                if (!ready) continue;
                active = provider;
                Debug.Log($"[Ads] Active provider: {provider.Name}");
                break;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Ads] Provider {provider.Name} failed to init: {e.Message}");
            }
        }
    }

    public bool TryShowRewarded(string placement, Action<bool> onResult)
    {
        foreach (IAdsProvider provider in ReadyProviders(p => p.RewardedReady))
        {
            provider.ShowRewarded(placement, onResult);
            return true;
        }
        onResult?.Invoke(false);
        return false;
    }

    public void MaybeShowInterstitial(string placement)
    {
        if (active == null || !active.InterstitialReady) return;
        if (Time.realtimeSinceStartup - lastInterstitialRealtime < interstitialMinInterval) return;
        runsSinceInterstitial++;
        if (runsSinceInterstitial < minRunsBetweenInterstitials) return;
        lastInterstitialRealtime = Time.realtimeSinceStartup;
        runsSinceInterstitial = 0;
        active.ShowInterstitial(placement);
    }

    private IEnumerable<IAdsProvider> ReadyProviders(Func<IAdsProvider, bool> formatCheck)
    {
        if (active != null && formatCheck(active)) yield return active;
        foreach (IAdsProvider provider in providers)
        {
            if (provider == active) continue;
            if (formatCheck(provider)) yield return provider;
        }
    }
}
