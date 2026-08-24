using System;
using System.Collections.Generic;
using Rubilovo.Logic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    private readonly List<IAdsProvider> providers = new();
    private IAdsProvider active;
    private float lastInterstitialRealtime = -9999f;
    private int rewardedWatchedToday;
    private int dayStampCache;

    public string ActiveProviderName => active?.Name ?? "none";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        providers.Add(new YandexAdsProvider());
        providers.Add(new VkAdsProvider());
    }

    private void Start()
    {
        RemoteAdConfig.Load();
        SaveSystem.TouchSession();
        dayStampCache = SaveSystem.Data.dayStamp;
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

    public bool RewardedAvailable =>
        active != null && active.RewardedReady && rewardedWatchedToday < RemoteAdConfig.Instance.rewardedDailyCap;

    public bool TryShowRewarded(string placement, Action<bool> onResult)
    {
        if (!RewardedAvailable)
        {
            onResult?.Invoke(false);
            return false;
        }
        foreach (IAdsProvider provider in ReadyProviders(p => p.RewardedReady))
        {
            provider.ShowRewarded(placement, result =>
            {
                if (result)
                {
                    RollDailyStamp();
                    rewardedWatchedToday++;
                }
                onResult?.Invoke(result);
            });
            return true;
        }
        onResult?.Invoke(false);
        return false;
    }

    public void MaybeShowInterstitial(string placement)
    {
        RemoteAdConfig cfg = RemoteAdConfig.Instance;

        if (cfg.firstSessionClean && SaveSystem.Data.sessionCount <= 1) return;
        bool unlockedBySessions = SaveSystem.Data.sessionCount >= cfg.interstitialMinSessionIndex;
        bool unlockedByPlaytime = SaveSystem.Data.totalPlaySeconds >= cfg.interstitialMinTotalPlaySeconds;
        if (!unlockedBySessions && !unlockedByPlaytime) return;
        if (SaveSystem.Data.runsToday < cfg.interstitialMinRunsBetween) return;
        if (Time.realtimeSinceStartup - lastInterstitialRealtime < cfg.interstitialCooldownSec) return;
        if (active == null || !active.InterstitialReady) return;

        lastInterstitialRealtime = Time.realtimeSinceStartup;
        active.ShowInterstitial(placement);
    }

    public void MaybeShowAppOpen()
    {
        if (!RemoteAdConfig.Instance.appOpenEnabled) return;
    }

    private void RollDailyStamp()
    {
        if (SaveSystem.Data.dayStamp == dayStampCache) return;
        dayStampCache = SaveSystem.Data.dayStamp;
        rewardedWatchedToday = 0;
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
