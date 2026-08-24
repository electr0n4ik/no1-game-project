#if YANDEX_MOBILEADS
using System;
using YandexMobileAds;
using YandexMobileAds.Base;

public class YandexAdsProvider : IAdsProvider
{
    private const string RewardedId = "demo-rewarded-yandex";
    private const string InterstitialId = "demo-interstitial-yandex";

    private RewardedAdLoader rewardedLoader;
    private RewardedAd rewardedAd;
    private InterstitialAdLoader interstitialLoader;
    private InterstitialAd interstitialAd;
    private Action<bool> pendingReward;
    private bool rewardEarned;

    public string Name => "Yandex";
    public bool RewardedReady => rewardedAd != null;
    public bool InterstitialReady => interstitialAd != null;

    public void Initialize(Action<bool> onReady)
    {
        YandexAds.SetUserConsent(true);
        YandexAds.SetLocationTracking(false);
        YandexAds.SetAgeRestricted(false);
        rewardedLoader = new RewardedAdLoader();
        interstitialLoader = new InterstitialAdLoader();
        rewardedLoader.LoadAd(new AdRequest(RewardedId), onLoaded: OnRewardedLoaded, onFailed: _ => PreloadRewardedLater());
        interstitialLoader.LoadAd(new AdRequest(InterstitialId), onLoaded: OnInterstitialLoaded, onFailed: _ => PreloadInterstitialLater());
        onReady?.Invoke(true);
    }

    public void ShowRewarded(string placement, Action<bool> onResult)
    {
        if (rewardedAd == null)
        {
            onResult?.Invoke(false);
            LoadRewarded();
            return;
        }
        pendingReward = onResult;
        rewardEarned = false;
        rewardedAd.Show();
    }

    public void ShowInterstitial(string placement)
    {
        if (interstitialAd == null) return;
        interstitialAd.Show();
    }

    private void OnRewardedLoaded(RewardedAd ad)
    {
        rewardedAd = ad;
        ad.OnRewarded += HandleRewarded;
        ad.OnAdShown += HandleShown;
        ad.OnAdDismissed += HandleDismissed;
        ad.OnAdFailedToShow += HandleShowFailed;
    }

    private void HandleRewarded(object sender, Reward args) => rewardEarned = true;

    private void HandleShown(object sender, EventArgs args) { }

    private void HandleDismissed(object sender, EventArgs args)
    {
        var ad = rewardedAd;
        rewardedAd = null;
        Unsubscribe(ad);
        ad.Destroy();
        var callback = pendingReward;
        pendingReward = null;
        bool earned = rewardEarned;
        rewardEarned = false;
        LoadRewarded();
        callback?.Invoke(earned);
    }

    private void HandleShowFailed(object sender, AdFailureEventArgs args)
    {
        var ad = rewardedAd;
        rewardedAd = null;
        Unsubscribe(ad);
        ad.Destroy();
        var callback = pendingReward;
        pendingReward = null;
        rewardEarned = false;
        LoadRewarded();
        callback?.Invoke(false);
    }

    private void Unsubscribe(RewardedAd ad)
    {
        ad.OnRewarded -= HandleRewarded;
        ad.OnAdShown -= HandleShown;
        ad.OnAdDismissed -= HandleDismissed;
        ad.OnAdFailedToShow -= HandleShowFailed;
    }

    private void LoadRewarded() =>
        rewardedLoader.LoadAd(new AdRequest(RewardedId), onLoaded: OnRewardedLoaded, onFailed: _ => PreloadRewardedLater());

    private void OnInterstitialLoaded(InterstitialAd ad)
    {
        interstitialAd = ad;
        ad.OnAdDismissed += (_, _) =>
        {
            ad.Destroy();
            interstitialAd = null;
            LoadInterstitial();
        };
        ad.OnAdFailedToShow += (_, _) =>
        {
            ad.Destroy();
            interstitialAd = null;
            LoadInterstitial();
        };
    }

    private void LoadInterstitial() =>
        interstitialLoader.LoadAd(new AdRequest(InterstitialId), onLoaded: OnInterstitialLoaded, onFailed: _ => PreloadInterstitialLater());

    private void PreloadRewardedLater() => Delay(LoadRewarded);

    private void PreloadInterstitialLater() => Delay(LoadInterstitial);

    private void Delay(Action action) => action();
}
#else
using System;
using UnityEngine;

public class YandexAdsProvider : IAdsProvider
{
    public string Name => "Yandex(stub)";
    public bool RewardedReady => Application.isEditor;
    public bool InterstitialReady => Application.isEditor;

    public void Initialize(Action<bool> onReady) => onReady?.Invoke(true);

    public void ShowRewarded(string placement, Action<bool> onResult)
    {
        Debug.Log($"[Ads][stub] Rewarded '{placement}' auto-granted");
        onResult?.Invoke(true);
    }

    public void ShowInterstitial(string placement) => Debug.Log($"[Ads][stub] Interstitial '{placement}'");
}
#endif
