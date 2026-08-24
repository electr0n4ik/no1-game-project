using System;

public class VkAdsProvider : IAdsProvider
{
    public string Name => "VK(stub)";
    public bool RewardedReady => false;
    public bool InterstitialReady => false;

    public void Initialize(Action<bool> onReady) => onReady?.Invoke(false);

    public void ShowRewarded(string placement, Action<bool> onResult) => onResult?.Invoke(false);

    public void ShowInterstitial(string placement) { }
}
