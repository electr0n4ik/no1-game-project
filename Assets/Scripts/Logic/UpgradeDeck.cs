using System.Collections.Generic;

namespace Rubilovo.Logic
{
    public enum CardType { NewWeapon = 0, UpgradeWeapon = 1, Passive = 2 }

    public struct UpgradeCard
    {
        public CardType Type;
        public WeaponId Weapon;
        public PassiveId Passive;
        public string Title;
    }

    public class LoadoutState
    {
        public readonly int[] WeaponLevels = new int[6];
        public readonly int[] PassiveLevels = new int[9];
        public int WeaponCount;
        public int PassiveCount;

        public bool HasWeapon(WeaponId id) => WeaponLevels[(int)id] > 0;

        public bool CanNewWeapon => WeaponCount < GameBalance.Player_WeaponSlots;

        public bool CanUpgradeWeapon(out WeaponId candidate)
        {
            candidate = default;
            if (WeaponCount == 0) return false;
            for (int i = 0; i < WeaponLevels.Length; i++)
            {
                if (WeaponLevels[i] > 0 && WeaponLevels[i] < WeaponsCatalog.MaxLevel)
                {
                    candidate = (WeaponId)i;
                    return true;
                }
            }
            return false;
        }

        public bool CanNewPassive => PassiveCount < GameBalance.Player_PassiveSlots;

        public bool CanUpgradePassive(out PassiveId candidate)
        {
            candidate = default;
            if (PassiveCount == 0) return false;
            for (int i = 0; i < PassiveLevels.Length; i++)
            {
                if (PassiveLevels[i] > 0 && PassiveLevels[i] < GameBalance.Passive_MaxLvl)
                {
                    candidate = (PassiveId)i;
                    return true;
                }
            }
            return false;
        }
    }

    public static class UpgradeDeck
    {
        private static readonly System.Random Rng = new();

        public static List<UpgradeCard> OfferThree(LoadoutState loadout, System.Random rng = null, int unlockedWeaponMask = 0x3F)
        {
            rng ??= Rng;
            var cards = new List<UpgradeCard>(3);
            for (int i = 0; i < 3; i++)
            {
                UpgradeCard card = RollOne(loadout, rng, unlockedWeaponMask, cards);
                if (card.Type != (CardType)(-1)) cards.Add(card);
            }
            return cards;
        }

        private static CardType RollType(double roll01)
        {
            double total = GameBalance.Card_Weights[0] + GameBalance.Card_Weights[1] + GameBalance.Card_Weights[2];
            double acc = GameBalance.Card_Weights[0] / total;
            if (roll01 < acc) return CardType.NewWeapon;
            if (roll01 < acc + (double)GameBalance.Card_Weights[1] / total) return CardType.UpgradeWeapon;
            return CardType.Passive;
        }

        private static UpgradeCard RollOne(LoadoutState s, System.Random rng, int unlockedMask, List<UpgradeCard> alreadyOffered)
        {
            CardType t = RollType(rng.NextDouble());
            for (int attempt = 0; attempt < 4; attempt++)
            {
                switch (t)
                {
                    case CardType.NewWeapon:
                    {
                        if (!s.CanNewWeapon) break;
                        var candidates = new List<WeaponId>();
                        for (int w = 0; w < 6; w++)
                        {
                            if (((unlockedMask >> w) & 1) != 0 && !s.HasWeapon((WeaponId)w))
                                candidates.Add((WeaponId)w);
                        }
                        foreach (UpgradeCard c in alreadyOffered)
                            candidates.Remove(c.Weapon);
                        if (candidates.Count > 0)
                        {
                            WeaponId pick = candidates[rng.Next(candidates.Count)];
                            return NewCard(CardType.NewWeapon, pick);
                        }
                        break;
                    }
                    case CardType.UpgradeWeapon:
                    {
                        var candidates = new List<WeaponId>();
                        for (int w = 0; w < 6; w++)
                        {
                            if (s.WeaponLevels[w] > 0 && s.WeaponLevels[w] < WeaponsCatalog.MaxLevel)
                                candidates.Add((WeaponId)w);
                        }
                        foreach (UpgradeCard c in alreadyOffered)
                            if (c.Type == CardType.UpgradeWeapon) candidates.Remove(c.Weapon);
                        if (candidates.Count > 0)
                        {
                            WeaponId pick = candidates[rng.Next(candidates.Count)];
                            return NewCard(CardType.UpgradeWeapon, pick);
                        }
                        break;
                    }
                    default:
                    {
                        var candidates = new List<PassiveId>();
                        for (int p = 0; p < 9; p++)
                        {
                            if (s.PassiveLevels[p] == 0 && !s.CanNewPassive) continue;
                            if (s.PassiveLevels[p] >= GameBalance.Passive_MaxLvl) continue;
                            bool dup = false;
                            foreach (UpgradeCard c in alreadyOffered)
                                if (c.Type == CardType.Passive && c.Passive == (PassiveId)p) dup = true;
                            if (!dup) candidates.Add((PassiveId)p);
                        }
                        if (candidates.Count > 0)
                        {
                            PassiveId pick = candidates[rng.Next(candidates.Count)];
                            return new UpgradeCard
                            {
                                Type = CardType.Passive,
                                Passive = pick,
                                Title = ((PassiveId)pick).ToString()
                            };
                        }
                        break;
                    }
                }
                t = (CardType)(((int)t + 1) % 3);
            }
            return new UpgradeCard { Type = (CardType)(-1) };
        }

        private static UpgradeCard NewCard(CardType type, WeaponId weapon)
        {
            return new UpgradeCard
            {
                Type = type,
                Weapon = weapon,
                Title = WeaponsCatalog.Names[(int)weapon]
            };
        }
    }
}
