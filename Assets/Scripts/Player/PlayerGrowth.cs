using System;
using Rubilovo.Logic;
using UnityEngine;

public class PlayerGrowth : MonoBehaviour
{
    [SerializeField] private PassiveEffects effects;

    public int Level { get; private set; } = 1;
    public int LevelUps { get; private set; }
    public float Xp { get; private set; }
    public float Scale => transform.localScale.x;
    public bool ScaleCapped => Scale >= GameBalance.Growth_ScaleCap;

    public event Action<int> OnLevelUp;
    public event Action OnXpChanged;

    private void Awake()
    {
        if (effects == null) effects = GetComponent<PassiveEffects>();
    }

    public float XpNeeded => XpCurve.Need(Level);

    public float MagnetRadius =>
        effects != null ? effects.MagnetRadius(Scale) : Kinematics.MagnetRadius(Scale, 0, 0);

    public void AddXp(float amount)
    {
        if (amount <= 0f || ScaleCapped && Level > 40) return;
        float gain = effects != null ? amount * effects.XpMult : amount;
        Xp += gain;
        RunTracker.Instance?.AddXp(gain);
        while (Xp >= XpNeeded)
        {
            Xp -= XpNeeded;
            Level++;
            LevelUps++;
            transform.localScale = Vector3.one * Kinematics.ScaleAfterLevelUps(LevelUps);
            OnLevelUp?.Invoke(Level);
        }
        OnXpChanged?.Invoke();
    }

    public void ResetRun()
    {
        Level = 1;
        LevelUps = 0;
        Xp = 0f;
        transform.localScale = Vector3.one;
        OnXpChanged?.Invoke();
    }
}
