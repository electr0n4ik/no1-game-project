namespace Rubilovo.Logic
{
    public static class Kinematics
    {
        public static float SizeMultiplier(float scale)
        {
            float baseMult = (float)System.Math.Pow(scale, -GameBalance.Move_SizeSlowExp);
            if (scale > GameBalance.Floor_ThresholdScale)
                return System.Math.Max(baseMult, GameBalance.Move_SpeedFloor);
            return baseMult;
        }

        public static float OrthoTarget(float scale)
        {
            float target = GameBalance.Cam_OrthoBase * (float)System.Math.Pow(scale, GameBalance.Cam_ZoomExp);
            return System.Math.Min(target, GameBalance.Cam_OrthoMax);
        }

        public static float OrthoLerp(float current, float target, float dt)
        {
            float t = 1f - (float)System.Math.Exp(-GameBalance.Cam_LerpK * dt);
            return current + (target - current) * t;
        }

        public static float SpeedBonus(int speedPassiveLvl, int metaSpeedSteps)
        {
            return 1f + GameBalance.Passive_PerLvlPct * speedPassiveLvl + 0.002f * metaSpeedSteps;
        }

        public static float FinalSpeed(float magnitude01, float scale, int speedPassiveLvl, int metaSpeedSteps)
        {
            return GameBalance.Move_BaseSpeed * System.Math.Clamp(magnitude01, 0f, 1f)
                   * SizeMultiplier(scale) * SpeedBonus(speedPassiveLvl, metaSpeedSteps);
        }

        public static float MagnetRadius(float scale, int magnetPassiveLvl, int metaMagnetSteps)
        {
            return GameBalance.Magnet_BaseRadius * scale
                   * (1f + GameBalance.Passive_PerLvlPct * magnetPassiveLvl)
                   * (1f + GameBalance.Meta_MagnetPctPerStep * metaMagnetSteps);
        }

        public static float ScaleAfterLevelUps(int levelUps)
        {
            float s = (float)System.Math.Pow(GameBalance.Growth_ScalePerLevel, levelUps);
            return System.Math.Min(s, GameBalance.Growth_ScaleCap);
        }

        public static int OverflowLevels(int levelUps) =>
            System.Math.Max(0, levelUps - GameBalance.Growth_MaxLevelUpsToCap);

        public static float PowerFromLevels(int levelUps) =>
            1f + GameBalance.Power_FromOverflowLevel * OverflowLevels(levelUps);
    }
}
