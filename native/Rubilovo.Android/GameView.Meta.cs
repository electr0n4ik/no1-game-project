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
    internal float _maxHp = GameBalance.Player_MaxHP;
    internal float _lastAward;
    internal string _toast = "";
    internal float _toastUntil;

    internal sealed class MetaSave
    {
        public long Meat;
        public int[] TreeSteps = new int[6];
        public float BestTime, BestSurv;
        public int BestKills, BestLevel, DayStamp = -1, RunsToday;
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
        EnsureDay();
    }

    private void SaveSave()
    {
        try { File.WriteAllText(SavePath, JsonSerializer.Serialize(_save)); } catch { }
    }

    private void EnsureDay()
    {
        int d = DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day;
        if (_save.DayStamp != d) { _save.DayStamp = d; _save.RunsToday = 0; }
    }

    private int MetaSteps(int branch) => _save.TreeSteps[branch];

    private int PassLvl(PassiveId id) => _loadout.PassiveLevels[(int)id];

    private float PowerMult =>
        (1f + GameBalance.Passive_PerLvlPct * PassLvl(PassiveId.Power))
        * (1f + GameBalance.Meta_AttackPctPerStep * MetaSteps(0))
        * Kinematics.PowerFromLevels(_levelUps);

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
        _maxHp = MaxHpNow;
        _hp = _maxHp;
    }

    private void AwardRunEnd()
    {
        EnsureDay();
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
