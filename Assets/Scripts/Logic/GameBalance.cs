namespace Rubilovo.Logic
{
    public static class GameBalance
    {
        public const string Version = "0.1.0";

        public const float Move_BaseSpeed = 4f;
        public const float Input_MaxPixels = 120f;
        public const float Input_DeadzonePx = 15f;
        public const float Move_SizeSlowExp = 0.5f;
        public const float Move_SpeedFloor = 0.62f;
        public const float Floor_ThresholdScale = 3f;
        public const float Contact_HitCooldownEnemy = 0.75f;

        public const float Cam_OrthoBase = 5f;
        public const float Cam_ZoomExp = 0.43f;
        public const float Cam_OrthoMax = 9.5f;
        public const float Cam_LerpK = 3f;

        public const float Arena_HalfSize = 20f;
        public const float Arena_Clamp = 19.2f;
        public const float Spawn_RingExtra = 1.5f;
        public const float Spawn_RerollMinDist = 6f;

        public const float Player_MaxHP = 100f;
        public const int Player_WeaponSlots = 4;
        public const int Player_PassiveSlots = 4;

        public const int Xp_Base = 5;
        public static readonly int[] Xp_StepTiers = { 10, 13, 16 };

        public const float Growth_ScalePerLevel = 1.06f;
        public const float Growth_ScaleCap = 4.5f;
        public const int Growth_MaxLevelUps = 26;

        public const float Magnet_BaseRadius = 1.7f;

        public const int Elite_BombXP = 30;
        public const int Boss_XP = 100;

        public static readonly int[] Card_Weights = { 25, 45, 30 };

        public const int Evo_ReqWeaponLvl = 5;
        public const int Evo_ReqPassiveLvl = 3;

        public const float Passive_PerLvlPct = 0.10f;
        public const float Passive_CooldownPerLvl = -0.08f;
        public const float Passive_ArmorFlatPerLvl = 1f;
        public const float Passive_RegenFlatPerLvl = 0.2f;
        public const int Passive_MaxLvl = 5;

        public const float Scale_HP_PerMinute = 1.10f;
        public const float Scale_DamageDivisorMinutes = 30f;

        public const float Spawn_Start = 0.8f;
        public const float Spawn_Decay = 0.97f;
        public const float Spawn_Floor = 0.15f;

        public const int Alive_CapEarly = 80;
        public const int Alive_CapLate = 120;
        public const float Cap_SwitchMin = 11f;

        public const float Run_DurationMin = 15f;

        public const int Revive_MaxPerRun = 1;
        public const float Revive_InvulnSec = 2f;

        public const float Meta_AttackPctPerStep = 0.05f;
        public const float Meta_VitalityPctPerStep = 0.075f;
        public const float Meta_ArmorFlatPerStep = 5f;
        public const float Meta_RegenPerStep = 0.25f;
        public const float Meta_SpeedPctPerStep = 0.01f;
        public const float Meta_MagnetPctPerStep = 0.075f;

        public static readonly string[] Tree_BranchNames = { "Атака", "Живучесть", "Броня", "Регенерация", "Скорость", "Магнит" };
        public const int Tree_BranchCount = 6;
        public const int Tree_StepsPerBranch = 16;
        public const int Tree_BaseCost = 100;
        public const double Tree_CostGrowth = 1.15;
        public static readonly int[] Tree_UnlockAfterTotalSteps = { 0, 0, 4, 8, 12, 16 };
    }
}
