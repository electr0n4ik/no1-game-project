namespace Rubilovo.Logic
{
    public enum WeaponId { Blades = 0, Daggers = 1, Axe = 2, Lightning = 3, Aura = 4, Whip = 5 }

    public struct WeaponLevelStats
    {
        public float Damage;
        public int Count;
        public float Radius;
        public float SpeedDeg;
        public float CooldownSec;
        public float UptimeSec;
        public float Extra;
    }

    public static class WeaponsCatalog
    {
        public const int MaxLevel = 5;
        public const float ProjectileSpeedDagger = 12f;
        public const float DaggerLifeSec = 1.2f;
        public const float DaggerTargetRadius = 8f;
        public const float AxeArcHeight = 3f;
        public const float AxeFanDegrees = 25f;
        public const float LightningTickBaseCd = 1.6f;
        public const float Aura_TickSec = 0.5f;
        public const float Aura_RehitSec = 1.0f;
        public const float Aura_Knockback = 0.3f;
        public const float Whip_HitVisualSec = 0.15f;
        public const float Blades_RehitSec = 0.25f;

        private static readonly WeaponLevelStats[][] Tables =
        {
            new[]
            {
                new WeaponLevelStats { Damage = 10f, Count = 2, Radius = 1.1f, SpeedDeg = 260f },
                new WeaponLevelStats { Damage = 14f, Count = 2, Radius = 1.1f, SpeedDeg = 260f },
                new WeaponLevelStats { Damage = 14f, Count = 3, Radius = 1.2f, SpeedDeg = 280f },
                new WeaponLevelStats { Damage = 18f, Count = 3, Radius = 1.3f, SpeedDeg = 280f, UptimeSec = 10f, Extra = 4f },
                new WeaponLevelStats { Damage = 22f, Count = 4, Radius = 1.4f, SpeedDeg = 300f, UptimeSec = 12f, Extra = 3f },
            },
            new[]
            {
                new WeaponLevelStats { Damage = 8f, Count = 1, CooldownSec = 1.00f },
                new WeaponLevelStats { Damage = 8f, Count = 2, CooldownSec = 1.00f },
                new WeaponLevelStats { Damage = 8f, Count = 2, CooldownSec = 0.85f },
                new WeaponLevelStats { Damage = 8f, Count = 3, CooldownSec = 0.85f },
                new WeaponLevelStats { Damage = 12f, Count = 4, CooldownSec = 0.85f, Extra = 1f },
            },
            new[]
            {
                new WeaponLevelStats { Damage = 20f, Count = 1, CooldownSec = 2.2f },
                new WeaponLevelStats { Damage = 20f, Count = 2, CooldownSec = 2.2f },
                new WeaponLevelStats { Damage = 26f, Count = 2, CooldownSec = 2.2f },
                new WeaponLevelStats { Damage = 26f, Count = 3, CooldownSec = 1.9f },
                new WeaponLevelStats { Damage = 32f, Count = 4, CooldownSec = 1.9f },
            },
            new[]
            {
                new WeaponLevelStats { Damage = 15f, Count = 1, CooldownSec = 1.6f, Radius = 1.2f },
                new WeaponLevelStats { Damage = 15f, Count = 2, CooldownSec = 1.6f, Radius = 1.2f },
                new WeaponLevelStats { Damage = 15f, Count = 2, CooldownSec = 1.3f, Radius = 1.2f },
                new WeaponLevelStats { Damage = 15f, Count = 3, CooldownSec = 1.3f, Radius = 1.2f },
                new WeaponLevelStats { Damage = 22f, Count = 3, CooldownSec = 1.3f, Radius = 1.5f },
            },
            new[]
            {
                new WeaponLevelStats { Damage = 4f, Radius = 1.6f },
                new WeaponLevelStats { Damage = 5f, Radius = 1.8f },
                new WeaponLevelStats { Damage = 6f, Radius = 2.0f },
                new WeaponLevelStats { Damage = 8f, Radius = 2.2f },
                new WeaponLevelStats { Damage = 10f, Radius = 2.4f, Extra = 0.15f },
            },
            new[]
            {
                new WeaponLevelStats { Damage = 12f, CooldownSec = 1.10f, Radius = 2.6f, Extra = 0.8f },
                new WeaponLevelStats { Damage = 16f, CooldownSec = 1.10f, Radius = 2.6f, Extra = 0.8f },
                new WeaponLevelStats { Damage = 16f, CooldownSec = 1.10f, Radius = 2.6f, Extra = 0.8f },
                new WeaponLevelStats { Damage = 20f, CooldownSec = 1.10f, Radius = 3.0f, Extra = 0.9f },
                new WeaponLevelStats { Damage = 24f, CooldownSec = 0.95f, Radius = 3.0f, Extra = 0.9f },
            },
        };

        public static readonly string[] Names =
        {
            "Орбитальные клинки", "Кинжалы", "Топор", "Молния", "Аура", "Хлыст"
        };

        public static WeaponLevelStats Stats(WeaponId id, int level)
        {
            int l = System.Math.Clamp(level, 1, MaxLevel);
            return Tables[(int)id][l - 1];
        }

        public static bool HasBackswing(WeaponId id, int level)
        {
            return id == WeaponId.Whip && level >= 3;
        }

        public static float BackswingDamagePct(WeaponId id, int level)
        {
            if (!HasBackswing(id, level)) return 0f;
            return level >= 5 ? 1f : 0.5f;
        }
    }

    public enum PassiveId { Magnet = 0, Speed = 1, Vitality = 2, Regen = 3, Cooldown = 4, Area = 5, Armor = 6, XpGain = 7, Power = 8 }

    public static class Evolutions
    {
        public struct Recipe
        {
            public WeaponId Weapon;
            public PassiveId RequiredPassive;
            public string ResultName;
        }

        public static readonly Recipe[] Table =
        {
            new Recipe { Weapon = WeaponId.Blades, RequiredPassive = PassiveId.Armor, ResultName = "Вихрь" },
            new Recipe { Weapon = WeaponId.Daggers, RequiredPassive = PassiveId.Area, ResultName = "Град стали" },
            new Recipe { Weapon = WeaponId.Axe, RequiredPassive = PassiveId.Speed, ResultName = "Секиропалач" },
            new Recipe { Weapon = WeaponId.Lightning, RequiredPassive = PassiveId.Cooldown, ResultName = "Гроза" },
            new Recipe { Weapon = WeaponId.Aura, RequiredPassive = PassiveId.Regen, ResultName = "Пустота" },
            new Recipe { Weapon = WeaponId.Whip, RequiredPassive = PassiveId.Vitality, ResultName = "Кровавый хлыст" },
        };

        public static bool TryFind(WeaponId weapon, out Recipe recipe)
        {
            foreach (Recipe r in Table)
            {
                if (r.Weapon == weapon) { recipe = r; return true; }
            }
            recipe = default;
            return false;
        }
    }
}
