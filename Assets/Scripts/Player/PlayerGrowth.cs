using System;
using UnityEngine;

public class PlayerGrowth : MonoBehaviour
{
    [SerializeField] private float xpBase = 5f;
    [SerializeField] private float xpStep = 3f;
    [SerializeField] private float maxScale = 4.5f;
    [SerializeField] private float scalePerLevel = 1.06f;
    [SerializeField] private OrbitWeapons weapons;

    public int Level { get; private set; } = 1;
    public float Xp { get; private set; }
    public float XpNeeded => xpBase + xpStep * Level;
    public OrbitWeapons Weapons => weapons;

    public event Action<int> OnLevelUp;
    public event Action OnXpChanged;

    public void AddXp(float amount)
    {
        if (amount <= 0f) return;
        Xp += amount;
        while (Xp >= XpNeeded && transform.localScale.x < maxScale)
        {
            Xp -= XpNeeded;
            Level++;
            Vector3 s = transform.localScale * scalePerLevel;
            transform.localScale = Vector3.Min(Vector3.one * maxScale, s);
            weapons.SetBladeCount(2 + Level / 3);
            OnLevelUp?.Invoke(Level);
        }
        OnXpChanged?.Invoke();
    }

    public void ResetRun()
    {
        Level = 1;
        Xp = 0f;
        transform.localScale = Vector3.one;
        weapons.SetBladeCount(2);
        OnXpChanged?.Invoke();
    }
}
