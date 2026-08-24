using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float max = 100f;
    [SerializeField] private bool isPlayer;

    public bool IsPlayer => isPlayer;
    public float Max => max;
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;
    public float ArmorFlat { get; set; }

    public event Action<Health> Damaged;
    public event Action<Health> Died;

    private void OnEnable()
    {
        Current = max;
    }

    public void SetMax(float value)
    {
        max = Mathf.Max(1f, value);
        if (Current > max) Current = max;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        float incoming = Mathf.Max(1f, amount - ArmorFlat);
        Current = Mathf.Max(0f, Current - incoming);
        Damaged?.Invoke(this);
        if (Current <= 0f) Died?.Invoke(this);
    }

    public void HealFlat(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Current = Mathf.Min(max, Current + amount);
    }

    public void FullHeal()
    {
        Current = max;
    }
}
