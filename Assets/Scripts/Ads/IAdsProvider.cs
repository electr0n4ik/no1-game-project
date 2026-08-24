using System;

public interface IAdsProvider
{
    string Name { get; }
    bool RewardedReady { get; }
    bool InterstitialReady { get; }
    void Initialize(Action<bool> onReady);
    void ShowRewarded(string placement, Action<bool> onResult);
    void ShowInterstitial(string placement);
}
