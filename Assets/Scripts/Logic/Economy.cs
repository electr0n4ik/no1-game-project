namespace Rubilovo.Logic
{
    public static class Economy
    {
        public static int MeatForRun(int kills, int elites, int bosses, float meatBonusPct = 0f)
        {
            double raw = 0.05 * kills + 8 * elites + 60 * bosses;
            return (int)System.Math.Floor(raw * (1.0 + meatBonusPct));
        }

        public static float AntiFarmDecay(int runIndexOfDay)
        {
            if (runIndexOfDay <= 3) return 1f;
            if (runIndexOfDay <= 5) return 0.5f;
            return 0.1f;
        }

        public static int MeatForRunFinal(int kills, int elites, int bosses, int runIndexOfDay, float meatBonusPct = 0f)
        {
            int baseMeat = MeatForRun(kills, elites, bosses, meatBonusPct);
            return (int)System.Math.Floor(baseMeat * AntiFarmDecay(runIndexOfDay));
        }

        public static int TreeStepCost(int stepIndexFrom1)
        {
            int m = System.Math.Max(1, stepIndexFrom1);
            return (int)System.Math.Floor(GameBalance.Tree_BaseCost * System.Math.Pow(GameBalance.Tree_CostGrowth, m - 1));
        }

        public static long TreeBranchTotalCost(int maxSteps = 16)
        {
            long sum = 0;
            for (int m = 1; m <= maxSteps; m++) sum += TreeStepCost(m);
            return sum;
        }

        public static long TreeFullTotalCost()
        {
            long sum = 0;
            for (int b = 0; b < GameBalance.Tree_BranchCount; b++)
                sum += TreeBranchTotalCost(GameBalance.Tree_StepsPerBranch);
            return sum;
        }
    }
}
