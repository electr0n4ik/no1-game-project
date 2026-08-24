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

    public event Action<Health> Damaged;
    public event Action<Health> Died;

    private void OnEnable()
    {
        Current = max;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Current = Mathf.Max(0f, Current - amount);
        Damaged?.Invoke(this);
        if (Current <= 0f) Died?.Invoke(this);
    }

    public void FullHeal()
    {
        Current = max;
    }
}
