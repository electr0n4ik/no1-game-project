namespace Rubilovo.Logic
{
    public static class XpCurve
    {
        public static int Need(int level)
        {
            int n = System.Math.Max(1, level);
            if (n < 20) return GameBalance.Xp_Base + GameBalance.Xp_StepTiers[0] * n;
            if (n < 40) return Need(19) + GameBalance.Xp_StepTiers[1] * (n - 19);
            return Need(39) + GameBalance.Xp_StepTiers[2] * (n - 39);
        }

        public static int TotalToReach(int targetLevel)
        {
            int sum = 0;
            for (int n = 1; n < targetLevel; n++) sum += Need(n);
            return sum;
        }
    }
}
