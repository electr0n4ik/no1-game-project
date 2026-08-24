using System.Collections.Generic;
using Rubilovo.Logic;
using UnityEngine;

[DefaultExecutionOrder(-70)]
public class RunTracker : MonoBehaviour
{
    public static RunTracker Instance { get; private set; }

    private readonly Dictionary<EnemyKind, int> killsByKind = new();
    public int TotalKills { get; private set; }
    public int ElitesKilled { get; private set; }
    public int BossesKilled { get; private set; }
    public double DamageDealt { get; private set; }
    public float XpCollected { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetRun()
    {
        killsByKind.Clear();
        TotalKills = 0;
        ElitesKilled = 0;
        BossesKilled = 0;
        DamageDealt = 0;
        XpCollected = 0;
    }

    public void RegisterKill(EnemyKind kind, bool isElite, bool isBoss)
    {
        TotalKills++;
        killsByKind.TryGetValue(kind, out int n);
        killsByKind[kind] = n + 1;
        if (isElite) ElitesKilled++;
        if (isBoss) BossesKilled++;
    }

    public IReadOnlyDictionary<EnemyKind, int> KillsByKind => killsByKind;

    public void AddDamage(double amount) => DamageDealt += amount;

    public void AddXp(float amount) => XpCollected += amount;

    public string KillsSummary()
    {
        var parts = new List<string>();
        foreach (KeyValuePair<EnemyKind, int> kv in killsByKind)
            parts.Add($"{kv.Key}: {kv.Value}");
        return parts.Count > 0 ? string.Join(", ", parts) : "—";
    }
}
