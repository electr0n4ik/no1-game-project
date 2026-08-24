using Rubilovo.Logic;
using UnityEngine;

public class PassiveEffects : MonoBehaviour
{
    private readonly int[] levels = new int[9];

    [field: SerializeField]
    public MetaBonuses Meta { get; private set; } = MetaBonuses.None();

    public int GetLevel(PassiveId id) => levels[(int)id];

    public bool CanRaise(PassiveId id) => levels[(int)id] < GameBalance.Passive_MaxLvl;

    public bool Raise(PassiveId id)
    {
        if (!CanRaise(id)) return false;
        bool isNew = levels[(int)id] == 0;
        levels[(int)id]++;
        if (id == PassiveId.Vitality)
        {
            var health = GetComponent<Health>();
            if (health != null) health.FullHeal();
        }
        _ = isNew;
        return true;
    }

    public float CooldownMult => System.Math.Max(0.2f,
        1f + GameBalance.Passive_CooldownPerLvl * levels[(int)PassiveId.Cooldown]);

    public float AreaMult => 1f + GameBalance.Passive_PerLvlPct * levels[(int)PassiveId.Area];

    public float PowerMult => (1f + GameBalance.Passive_PerLvlPct * levels[(int)PassiveId.Power])
                              * (1f + Meta.AttackPct);

    public float SpeedBonus => Kinematics.SpeedBonus(levels[(int)PassiveId.Speed], Meta.SpeedSteps);

    public float MagnetRadius(float scale) =>
        Kinematics.MagnetRadius(scale, levels[(int)PassiveId.Magnet], Meta.MagnetSteps);

    public float XpMult => 1f + GameBalance.Passive_PerLvlPct * levels[(int)PassiveId.XpGain];

    public float ArmorFlat =>
        GameBalance.Passive_ArmorFlatPerLvl * levels[(int)PassiveId.Armor] + Meta.ArmorFlat;

    public float RegenPerSec =>
        GameBalance.Passive_RegenFlatPerLvl * levels[(int)PassiveId.Regen] + Meta.RegenFlat;

    public float MaxHp(float baseHp) => baseHp * (1f + GameBalance.Passive_PerLvlPct * levels[(int)PassiveId.Vitality] + Meta.VitalityPct);
}

[System.Serializable]
public struct MetaBonuses
{
    public float AttackPct;
    public float VitalityPct;
    public float ArmorFlat;
    public float RegenFlat;
    public int SpeedSteps;
    public int MagnetSteps;

    public static MetaBonuses None() => new()
    {
        AttackPct = 0f, VitalityPct = 0f, ArmorFlat = 0f,
        RegenFlat = 0f, SpeedSteps = 0, MagnetSteps = 0
    };
}
