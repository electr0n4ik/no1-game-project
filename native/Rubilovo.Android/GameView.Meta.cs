using Android.Graphics;
using Android.Views;
using Rubilovo.Logic;
using System.Text.Json;

namespace Rubilovo.Android;

public partial class GameView
{
    internal MetaSave _save = new();
    internal LoadoutState _loadout = new();
    internal int _pendingCards;
    internal readonly List<UpgradeCard> _cards = new();
    internal readonly RectF?[] _cardRects = new RectF?[3];
    internal readonly RectF?[] _claimRects = new RectF?[3];
    internal float _maxHp = GameBalance.Player_MaxHP;
    internal float _lastAward;
    internal bool _reviveMeatUsed;
    internal string _toast = "";
    internal float _toastUntil;

    internal sealed class MetaSave
    {
        public long Meat;
        public int[] TreeSteps = new int[6];
        public float BestTime, BestSurv;
        public int BestKills, BestLevel, DayStamp = -1, RunsToday;
        public bool SndOn = true, VibOn = true;
        public int InstallDay, QuestDay, QuestsDoneToday, StreakDay = 1, StreakLastStamp = -1;
        public string[] QuestKeys = new string[3];
        public int[] QuestTargets = new int[3], QuestRewards = new int[3], QuestProg = new int[3];
        public bool[] QuestClaimed = new bool[3];
        public double BankSec;
        public long FreeChestAt;
        public int PendingBuffType, RerollsNext;
        public double LastSeenUtc;
        public float PendingBuffVal;
    }

    private string SavePath => System.IO.Path.Combine(Context!.FilesDir!.Path!, "rubilovo_save.json");

    private void LoadSave()
    {
        try
        {
            if (File.Exists(SavePath))
                _save = JsonSerializer.Deserialize<MetaSave>(File.ReadAllText(SavePath)) ?? new MetaSave();
        }
        catch { _save = new MetaSave(); }
        if (_save.InstallDay == 0)
            _save.InstallDay = DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day;
        if (_save.QuestKeys == null)
        {
            _save.QuestKeys = new string[3];
            _save.QuestTargets = new int[3]; _save.QuestRewards = new int[3];
            _save.QuestProg = new int[3]; _save.QuestClaimed = new bool[3];
        }
        if (_save.LastSeenUtc > 0)
        {
            double delta = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _save.LastSeenUtc;
            if (delta > 300) _save.BankSec = Math.Min(28800, _save.BankSec + Math.Min(delta, 172800));
        }
        EnsureDay();
        EnsureQuests();
    }

    private void SaveSave()
    {
        try { File.WriteAllText(SavePath, JsonSerializer.Serialize(_save)); } catch { }
    }

    private void EnsureQuests()
    {
        int today = DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day;
        if (_save.StreakLastStamp == -1) _save.StreakLastStamp = today;
        int daysMissed = Rubilovo.Logic.Streak.DaysBetween(_save.StreakLastStamp, today);
        if (daysMissed >= 2) _save.StreakDay = 1;
        if (_save.QuestDay == today) return;
        _save.QuestDay = today;
        _save.QuestsDoneToday = 0;
        int daysSinceInstall = Rubilovo.Logic.Streak.DaysBetween(_save.InstallDay, today);
        var quests = Rubilovo.Logic.QuestGen.Generate(today, Rubilovo.Logic.QuestGen.TierByInstallDay(daysSinceInstall), 0xFF);
        for (int i = 0; i < 3 && i < quests.Length; i++)
        {
            _save.QuestKeys[i] = quests[i].Key;
            _save.QuestTargets[i] = quests[i].Target;
            _save.QuestRewards[i] = quests[i].Reward;
            _save.QuestProg[i] = 0;
            _save.QuestClaimed[i] = false;
        }
        SaveSave();
    }

    private void AddQuestProg(string key, int amount)
    {
        EnsureQuests();
        for (int i = 0; i < 3; i++)
        {
            if (_save.QuestKeys[i] != key || _save.QuestClaimed[i]) continue;
            _save.QuestProg[i] += amount;
        }
    }

    private bool ClaimQuest(int i)
    {
        if (_save.QuestClaimed[i] || _save.QuestProg[i] < _save.QuestTargets[i]) return false;
        _save.QuestClaimed[i] = true;
        _save.Meat += _save.QuestRewards[i];
        _save.QuestsDoneToday++;
        if (_save.QuestsDoneToday >= 2 && _save.StreakLastStamp != TodayStamp())
        {
            int today = TodayStamp();
            int daysMissed = Rubilovo.Logic.Streak.DaysBetween(_save.StreakLastStamp, today);
            _save.StreakDay = daysMissed >= 2 ? 1 : Rubilovo.Logic.Streak.Advance(_save.StreakDay);
            int ep = Rubilovo.Logic.Streak.RewardFor(_save.StreakDay);
            if (ep < 0)
            {
                int epic = Rubilovo.Logic.Streak.EpicRoll(_rng.Next(100));
                _save.Meat += epic;
                Toast($"СТРИК ДЕНЬ 7 · ЭПИК +{epic} 🍖");
            }
            else
            {
                _save.Meat += ep;
                Toast($"СТРИК ДЕНЬ {_save.StreakDay} · +{ep} 🍖");
            }
            _save.StreakLastStamp = today;
        }
        SaveSave();
        return true;
    }

    private long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private int BankMeat => (int)(_save.BankSec / 36.0);

    private void ClaimBank()
    {
        int m = BankMeat;
        if (m <= 0) return;
        _save.Meat += m;
        _save.BankSec = 0;
        SaveSave();
        Toast($"БАНК: +{m} 🍖");
    }

    private void OpenFreeChest()
    {
        if (NowUnix() < _save.FreeChestAt) return;
        int roll = _rng.Next(100);
        if (roll < 55) { _save.PendingBuffType = 1; _save.PendingBuffVal = 0.20f; Toast("СУНДУК: +20% урона на след. забег"); }
        else if (roll < 80) { _save.PendingBuffType = 2; _save.PendingBuffVal = 2; Toast("СУНДУК: +2 реролла на след. забег"); }
        else if (roll < 95) { _save.PendingBuffType = 3; _save.PendingBuffVal = 30; Toast("СУНДУК: щит 30 сек на старте"); }
        else { int m = 100 + _rng.Next(51); _save.Meat += m; Toast($"СУНДУК: +{m} 🍖"); }
        _save.FreeChestAt = NowUnix() + 4 * 3600;
        SaveSave();
    }

    private int TodayStamp() => DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day;

    private void EnsureDay()
    {
        int d = DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day;
        if (_save.DayStamp != d) { _save.DayStamp = d; _save.RunsToday = 0; }
    }

    private int MetaSteps(int branch) => _save.TreeSteps[branch];

    private int PassLvl(PassiveId id) => _loadout.PassiveLevels[(int)id];

    internal float _dmgBuff = 1f;

    private float PowerMult =>
        (1f + GameBalance.Passive_PerLvlPct * PassLvl(PassiveId.Power))
        * (1f + GameBalance.Meta_AttackPctPerStep * MetaSteps(0))
        * Kinematics.PowerFromLevels(_levelUps)
        * _dmgBuff;

    private float CooldownMult => MathF.Max(0.2f, 1f + GameBalance.Passive_CooldownPerLvl * PassLvl(PassiveId.Cooldown));

    private float AreaMult => 1f + GameBalance.Passive_PerLvlPct * PassLvl(PassiveId.Area);

    private float ArmorFlat =>
        GameBalance.Passive_ArmorFlatPerLvl * PassLvl(PassiveId.Armor)
        + GameBalance.Meta_ArmorFlatPerStep * MetaSteps(2);

    private float RegenPerSec =>
        GameBalance.Passive_RegenFlatPerLvl * PassLvl(PassiveId.Regen)
        + GameBalance.Meta_RegenPerStep * MetaSteps(3);

    private float MaxHpNow =>
        GameBalance.Player_MaxHP * (1f + GameBalance.Passive_PerLvlPct * PassLvl(PassiveId.Vitality)
        + GameBalance.Meta_VitalityPctPerStep * MetaSteps(1));

    private float XpMult => 1f + GameBalance.Passive_PerLvlPct * PassLvl(PassiveId.XpGain);

    private void InitRunLoadout()
    {
        _loadout = new LoadoutState();
        _loadout.WeaponLevels[(int)WeaponId.Blades] = 1;
        _loadout.WeaponCount = 1;
        _bladeStats = WeaponsCatalog.Stats(WeaponId.Blades, 1);
        _reviveMeatUsed = false;
        _maxHp = MaxHpNow;
        _hp = _maxHp;
    }

    private void AwardRunEnd()
    {
        EnsureDay();
        EnsureQuests();
        AddQuestProg("runs", 1);
        AddQuestProg("kills", _kills);
        AddQuestProg("minutes", (int)(_runTime / 60f));
        AddQuestProg("bosses", _bossesKilled);
        AddQuestProg("elites", _elites);
        int meat = Economy.MeatForRunFinal(_kills, _elites, _bossesKilled, _save.RunsToday + 1);
        _save.Meat += meat;
        _lastAward = meat;
        if (_runTime > _save.BestTime) _save.BestTime = _runTime;
        if (_kills > _save.BestKills) _save.BestKills = _kills;
        if (_level > _save.BestLevel) _save.BestLevel = _level;
        if (_survival && _runTime > _save.BestSurv) _save.BestSurv = _runTime;
        _save.RunsToday++;
        SaveSave();
    }

    private void ReviveForMeat()
    {
        const int cost = 250;
        if (_save.Meat < cost || _reviveMeatUsed) return;
        _save.Meat -= cost;
        _reviveMeatUsed = true;
        AddQuestProg("meat_spent", cost);
        SaveSave();
        ReviveNow();
    }

    private void ReviveNow()
    {
        _hp = _maxHp;
        _lastHurt = _runTime;
        _shots.Clear();
        _phase = Phase.Run;
    }

    private long TreeCost(int branch) =>
        Economy.TreeStepCost(Math.Min(_save.TreeSteps[branch] + 1, GameBalance.Tree_StepsPerBranch));

    private bool TreeUnlocked(int branch) =>
        _save.TreeSteps.Sum() >= GameBalance.Tree_UnlockAfterTotalSteps[branch];

    private bool TreeMaxed(int branch) => _save.TreeSteps[branch] >= GameBalance.Tree_StepsPerBranch;

    private bool TryBuyBranch(int branch)
    {
        if (!TreeUnlocked(branch) || TreeMaxed(branch)) return false;
        long cost = TreeCost(branch);
        if (_save.Meat < cost) return false;
        _save.Meat -= cost;
        _save.TreeSteps[branch]++;
        SaveSave();
        return true;
    }
}
