using System;
using System.IO;
using Rubilovo.Logic;
using UnityEngine;

public static class AdPlacements
{
    public const string Revive = "revive";
    public const string DoubleRunMeat = "x2_run_meat";
    public const string DoubleBank = "x2_offline_bank";
    public const string DoubleChest = "x2_chest";
}

[Serializable]
public class RemoteAdConfig
{
    public bool firstSessionClean = true;
    public int interstitialCooldownSec = 180;
    public int interstitialMinRunsBetween = 2;
    public int interstitialMinSessionIndex = 2;
    public double interstitialMinTotalPlaySeconds = 300.0;
    public int rewardedDailyCap = 8;
    public bool appOpenEnabled = false;
    public int appOpenMinSessionIndex = 4;
    public double appOpenBackgroundGapMinutes = 45.0;

    public static RemoteAdConfig Instance { get; private set; } = new();

    public static void Load()
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "rubilovo_remote.json");
            if (File.Exists(path))
                Instance = JsonUtility.FromJson<RemoteAdConfig>(File.ReadAllText(path)) ?? new RemoteAdConfig();
        }
        catch (Exception)
        {
            Instance = new RemoteAdConfig();
        }
    }
}
