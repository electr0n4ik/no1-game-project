namespace Rubilovo.Logic
{
    public static class XpCurve
    {
        public static int StepFor(int level)
        {
            for (int t = 0; t < GameBalance.Xp_TierBreaks.Length; t++)
                if (level < GameBalance.Xp_TierBreaks[t])
                    return GameBalance.Xp_StepTiers[t];
            return GameBalance.Xp_StepTiers[^1];
        }

        public static int Need(int level)
        {
            int n = System.Math.Max(1, System.Math.Min(level, GameBalance.Xp_MaxLevel));
            if (n == 1) return GameBalance.Xp_Base + StepFor(1);
            return Need(n - 1) + StepFor(n);
        }

        public static long TotalToReach(int targetLevel)
        {
            long sum = 0;
            for (int n = 1; n < targetLevel && n < GameBalance.Xp_MaxLevel; n++) sum += Need(n);
            return sum;
        }
    }
}
