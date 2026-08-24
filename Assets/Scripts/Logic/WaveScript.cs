using System.Collections.Generic;

namespace Rubilovo.Logic
{
    public enum EnemyKind { Walker = 0, Runner = 1, Tank = 2, Shooter = 3, Sprinter = 4, Kamikaze = 5 }

    public struct EnemyBaseStats
    {
        public float Hp, Speed, Damage;
    }

    public struct BossStats
    {
        public int Minute;
        public float Hp, ContactDamage, Speed;
    }

    public static class WaveScript
    {
        private static readonly EnemyBaseStats[] Bases =
        {
            new EnemyBaseStats { Hp = 18f, Speed = 1.2f, Damage = 8f },
            new EnemyBaseStats { Hp = 10f, Speed = 3.0f, Damage = 6f },
            new EnemyBaseStats { Hp = 60f, Speed = 0.8f, Damage = 14f },
            new EnemyBaseStats { Hp = 14f, Speed = 1.0f, Damage = 6f },
            new EnemyBaseStats { Hp = 8f, Speed = 2.6f, Damage = 5f },
            new EnemyBaseStats { Hp = 6f, Speed = 3.4f, Damage = 18f },
        };

        private static readonly BossStats[] Bosses =
        {
            new BossStats { Minute = 4, Hp = 800f, ContactDamage = 14f, Speed = 1.6f },
            new BossStats { Minute = 8, Hp = 1150f, ContactDamage = 15f, Speed = 1.3f },
            new BossStats { Minute = 12, Hp = 1700f, ContactDamage = 17f, Speed = 1.8f },
            new BossStats { Minute = 15, Hp = 2300f, ContactDamage = 18f, Speed = 1.5f },
        };

        public static EnemyBaseStats Base(EnemyKind kind) => Bases[(int)kind];

        public static readonly float[] BossScheduleMinutes = { 4f, 8f, 12f, 15f };

        public static BossStats BossByIndex(int index)
        {
            int i = index < 0 ? 0 : (index >= Bosses.Length ? Bosses.Length - 1 : index);
            return Bosses[i];
        }

        public static int BossCount => Bosses.Length;

        public static bool TryBossAtMinute(float elapsedMinutes, out BossStats boss)
        {
            for (int i = 0; i < Bosses.Length; i++)
            {
                if (System.Math.Abs(Bosses[i].Minute - elapsedMinutes) < 0.01 ||
                    (elapsedMinutes >= Bosses[i].Minute && elapsedMinutes < Bosses[i].Minute + 0.02))
                {
                    boss = Bosses[i];
                    return true;
                }
            }
            boss = default;
            return false;
        }

        public static bool IsBossWindow(float elapsedMinutes, out BossStats boss)
        {
            foreach (BossStats b in Bosses)
            {
                if (elapsedMinutes >= b.Minute && elapsedMinutes < b.Minute + WaveConstants.BossPauseMinutes)
                {
                    boss = b;
                    return true;
                }
            }
            boss = default;
            return false;
        }

        public static float ScaledHp(float elapsedMinutes, EnemyKind kind)
        {
            return Base(kind).Hp * (float)System.Math.Pow(GameBalance.Scale_HP_PerMinute, elapsedMinutes);
        }

        public static float ScaledDamage(float elapsedMinutes, EnemyKind kind)
        {
            return Base(kind).Damage * (1f + elapsedMinutes / 30f);
        }

        public static float SpawnInterval(int spawnNumber)
        {
            return System.Math.Max(GameBalance.Spawn_Floor,
                GameBalance.Spawn_Start * (float)System.Math.Pow(GameBalance.Spawn_Decay, spawnNumber));
        }

        public static int BatchSize(float elapsedMinutes)
        {
            float m = elapsedMinutes;
            if (m < 3f) return 1;
            if (m < 7f) return 2;
            if (m < 8f) return 3;
            if (m < 11f) return 3;
            if (m < 12f) return 3;
            return 4;
        }

        public static int AliveCap(float elapsedMinutes)
        {
            return elapsedMinutes < GameBalance.Cap_SwitchMin ? GameBalance.Alive_CapEarly : GameBalance.Alive_CapLate;
        }

        public static List<EnemyKind> RollWaveComposition(float elapsedMinutes, int count, System.Random rng)
        {
            var result = new List<EnemyKind>(count);
            for (int i = 0; i < count; i++) result.Add(RollKind(elapsedMinutes, rng.NextDouble()));
            return result;
        }

        public static EnemyKind RollKind(float elapsedMinutes, double roll01)
        {
            float m = elapsedMinutes;
            if (m < 1f) return EnemyKind.Walker;
            if (m < 3f) return roll01 < 0.70 ? EnemyKind.Walker : EnemyKind.Runner;
            if (m < 4f) return Pick(roll01, 0.50, 0.80, EnemyKind.Walker, EnemyKind.Runner, EnemyKind.Sprinter);
            if (m < 7f) return Pick(roll01, 0.40, 0.70, EnemyKind.Walker, EnemyKind.Runner, EnemyKind.Shooter);
            if (m < 8f) return Pick(roll01, 0.35, 0.60, EnemyKind.Walker, EnemyKind.Runner, EnemyKind.Tank);
            if (m < 11f) return Pick(roll01, 0.25, 0.55, EnemyKind.Runner, EnemyKind.Shooter, EnemyKind.Tank);
            return Pick(roll01, 0.20, 0.45, EnemyKind.Runner, EnemyKind.Tank, EnemyKind.Kamikaze);
        }

        private static EnemyKind Pick(double roll, double t1, double t2, EnemyKind a, EnemyKind b, EnemyKind c)
        {
            if (roll < t1) return a;
            if (roll < t2) return b;
            return c;
        }

        public static class WaveConstants
        {
            public const float BossPauseMinutes = 1f;
            public const float Elite_TimerMinSec = 60f;
            public const float Elite_TimerMaxSec = 90f;
            public const float Elite_FromMinute = 3f;
            public const float Elite_HpMult = 6f;
            public const float Elite_DamageMult = 1.5f;
            public const float Elite_SizeMult = 1.4f;
            public const float Elite_SpeedMult = 0.9f;

            public const float Shooter_PreferredDist = 5f;
            public const float Shooter_FireCooldown = 2.5f;
            public const float Shooter_ProjectileSpeed = 6f;
            public const float Shooter_ProjectileLife = 3f;
            public const int Sprinter_PackMin = 5;
            public const int Sprinter_PackMax = 7;
            public const float Kamikaze_TelegraphSec = 0.5f;
            public const float Kamikaze_ExplosionRadius = 1.2f;

            public const float Boss_HpMultOfRow = 30f;
            public const float Boss_DamageMultOfRow = 1.5f;
            public const float Dash_TelegraphSec = 0.8f;
            public const float Dash_Speed = 9f;
            public const float Dash_Cooldown = 6f;
            public const float Ring_TelegraphSec = 1.0f;
            public const int Ring_Projectiles = 12;
            public const float Ring_ProjectileSpeed = 5f;
        }
    }
}
