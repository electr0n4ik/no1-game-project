using System;
using Rubilovo.Logic;
using UnityEngine;

public enum TreeBranch { Attack = 0, Vitality = 1, Armor = 2, Regen = 3, Speed = 4, Magnet = 5 }

public static class MetaTree
{
    public static int MaxStepsPerBranch => GameBalance.Tree_StepsPerBranch;

    public static int Steps(TreeBranch branch) => SaveSystem.Data.treeSteps[(int)branch];

    public static bool IsUnlocked(TreeBranch branch)
    {
        int total = 0;
        foreach (int s in SaveSystem.Data.treeSteps) total += s;
        return total >= GameBalance.Tree_UnlockAfterTotalSteps[(int)branch];
    }

    public static long NextCost(TreeBranch branch) =>
        Economy.TreeStepCost(Math.Min(Steps(branch) + 1, MaxStepsPerBranch));

    public static bool TryBuy(TreeBranch branch)
    {
        if (!IsUnlocked(branch)) return false;
        if (Steps(branch) >= MaxStepsPerBranch) return false;
        if (!SaveSystem.TrySpendMeat(NextCost(branch))) return false;
        SaveSystem.Data.treeSteps[(int)branch]++;
        SaveSystem.Save();
        return true;
    }

    public static MetaBonuses ComputeBonuses()
    {
        int[] s = SaveSystem.Data.treeSteps;
        return new MetaBonuses
        {
            AttackPct = GameBalance.Meta_AttackPctPerStep * s[(int)TreeBranch.Attack],
            VitalityPct = GameBalance.Meta_VitalityPctPerStep * s[(int)TreeBranch.Vitality],
            ArmorFlat = GameBalance.Meta_ArmorFlatPerStep * s[(int)TreeBranch.Armor],
            RegenFlat = GameBalance.Meta_RegenPerStep * s[(int)TreeBranch.Regen],
            SpeedSteps = s[(int)TreeBranch.Speed],
            MagnetSteps = s[(int)TreeBranch.Magnet]
        };
    }
}
