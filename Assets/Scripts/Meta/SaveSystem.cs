using System;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int version = 1;
    public long meat;
    public int[] treeSteps = new int[6];
    public int[] unlockedWeapons = { 0, 1 };
    public int sessionCount;
    public double totalPlaySeconds;
    public int dayStamp;
    public int runsToday;
    public float bestTimeSec;
    public int bestKills;
    public int bestLevel;
    public double lastSeenUtc;
    public float bestSurvivalTimeSec;
}

public static class SaveSystem
{
    private const string Key = "rubilovo_save_v1";

    private static SaveData cache;

    public static SaveData Data => cache ??= Load();

    private static SaveData Load()
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (json.Length == 0) return new SaveData();
        try
        {
            return JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }
        catch (ArgumentException)
        {
            return new SaveData();
        }
    }

    public static void Save()
    {
        Data.lastSeenUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
    }

    public static void TouchSession()
    {
        Data.sessionCount++;
        EnsureDay();
        Save();
    }

    public static void EnsureDay()
    {
        int stamp = NowStamp();
        if (Data.dayStamp == stamp) return;
        Data.dayStamp = stamp;
        Data.runsToday = 0;
    }

    public static int RunIndexOfNext() => Data.runsToday + 1;

    public static void RegisterRunEnd(double playSecondsDelta)
    {
        EnsureDay();
        Data.runsToday++;
        Data.totalPlaySeconds += playSecondsDelta;
        Save();
    }

    public static void AddMeat(long amount)
    {
        if (amount <= 0) return;
        Data.meat += amount;
        Save();
    }

    public static bool TrySpendMeat(long cost)
    {
        if (cost <= 0 || Data.meat < cost) return false;
        Data.meat -= cost;
        Save();
        return true;
    }

    public static void RecordSurvivalBest(float timeSec)
    {
        if (timeSec <= Data.bestSurvivalTimeSec) return;
        Data.bestSurvivalTimeSec = timeSec;
        Save();
    }

    public static void RecordBest(float timeSec, int kills, int level)
    {
        bool changed = false;
        if (timeSec > Data.bestTimeSec) { Data.bestTimeSec = timeSec; changed = true; }
        if (kills > Data.bestKills) { Data.bestKills = kills; changed = true; }
        if (level > Data.bestLevel) { Data.bestLevel = level; changed = true; }
        if (changed) Save();
    }

    public static int UnlockedWeaponsMask()
    {
        int mask = 0;
        foreach (int w in Data.unlockedWeapons) mask |= 1 << w;
        return mask == 0 ? 0x3 : mask;
    }

    private static int NowStamp()
    {
        DateTime t = DateTime.Now;
        return t.Year * 10000 + t.Month * 100 + t.Day;
    }
}
