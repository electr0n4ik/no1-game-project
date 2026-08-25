using System.Collections.Generic;

namespace Rubilovo.Logic
{
    public sealed class Quest
    {
        public string Key = "";
        public string Title = "";
        public int Target;
        public int Reward;
    }

    public static class QuestGen
    {
        public sealed class Template
        {
            public string Key = "";
            public string Title = "";
            public int[] Targets = { 0, 0, 0 };
            public int[] Rewards = { 0, 0, 0 };
        }

        public static readonly Template[] Pool =
        {
            new Template{ Key="kills",      Title="Убей {0} врагов за день",            Targets=new[]{500,800,1200}, Rewards=new[]{50,75,100} },
            new Template{ Key="minutes",    Title="проживи {0} минут за день",          Targets=new[]{15,25,35},     Rewards=new[]{50,75,100} },
            new Template{ Key="chests",     Title="открой {0} сундука за день",         Targets=new[]{3,5,6},        Rewards=new[]{75,100,125} },
            new Template{ Key="runs",       Title="заверши {0} забега",                 Targets=new[]{2,3,4},        Rewards=new[]{50,75,100} },
            new Template{ Key="bosses",     Title="убей {0} боссов",                    Targets=new[]{2,4,6},        Rewards=new[]{75,100,125} },
            new Template{ Key="orbs",       Title="собери {0} сфер опыта",              Targets=new[]{400,800,1200}, Rewards=new[]{50,75,100} },
            new Template{ Key="meat_spent", Title="потрать {0} мяса в дереве",          Targets=new[]{500,1000,1500}, Rewards=new[]{75,100,125} },
            new Template{ Key="elites",     Title="убей {0} элиток",                    Targets=new[]{10,20,30},     Rewards=new[]{75,100,125} },
        };

        public static int TierByInstallDay(int daysSinceInstall) =>
            daysSinceInstall >= 10 ? 2 : daysSinceInstall >= 4 ? 1 : 0;

        public static Quest[] Generate(int seed, int tier, int availableMask)
        {
            int ti = System.Math.Clamp(tier, 0, 2);
            ulong x = (ulong)seed * 6364136223846793005UL + 1442695040888963407UL;
            var picked = new List<int>();
            var quests = new List<Quest>();
            int guard = 0;
            while (quests.Count < 3 && guard++ < 64)
            {
                x = x * 6364136223846793005UL + 1442695040888963407UL;
                int idx = (int)((x >> 33) % (ulong)Pool.Length);
                if (picked.Contains(idx)) continue;
                if ((availableMask & (1 << idx)) == 0) continue;
                picked.Add(idx);
                Template t = Pool[idx];
                quests.Add(new Quest
                {
                    Key = t.Key,
                    Title = string.Format(t.Title, t.Targets[ti]),
                    Target = t.Targets[ti],
                    Reward = t.Rewards[ti]
                });
            }
            return quests.ToArray();
        }
    }

    public static class Streak
    {
        public static int RewardFor(int day) => day switch
        {
            1 => 50, 2 => 60, 3 => 75, 4 => 90, 5 => 110, 6 => 130,
            _ => -1
        };

        public const int EpicDay = 7;

        public static int Advance(int completedDay)
        {
            return completedDay >= EpicDay ? 1 : completedDay + 1;
        }

        public static int EpicRoll(int roll01to99)
        {
            int r = System.Math.Clamp(roll01to99, 0, 99);
            if (r < 50) return 700;
            if (r < 85) return 800;
            return 900;
        }

        public static int DaysBetween(int stampA, int stampB)
        {
            var a = FromStamp(stampA);
            var b = FromStamp(stampB);
            return (int)(b.Date - a.Date).TotalDays;
        }

        private static System.DateTime FromStamp(int stamp) =>
            new System.DateTime(stamp / 10000, stamp / 100 % 100, stamp % 100);
    }
}
