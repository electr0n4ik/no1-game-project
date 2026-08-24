using Android.Graphics;
using Rubilovo.Logic;

namespace Rubilovo.Android;

public partial class GameView
{
    internal WeaponLevelStats _bladeStats = WeaponsCatalog.Stats(WeaponId.Blades, 1);
    internal bool _bladesOn = true;
    internal int _bladesN = 2;
    internal float _bladeR = 1.1f;
    internal float _magnetR = 1.7f;
    internal float _bladeUptime;
    internal float _dirX = 1f, _dirY;

    private sealed class PBullet
    {
        public float X, Y, Vx, Vy, Life, Damage;
        public int Pierce = -1;
        public bool Arc;
        public HashSet<Mob> Hit = new();
    }

    private readonly List<PBullet> _pshots = new();

    private sealed class DmgNum { public float X, Y; public string Txt = ""; public float Life; public bool Crit; }
    private readonly List<DmgNum> _nums = new();

    private sealed class Part { public float X, Y, Vx, Vy, Life; public byte R, G, B; }
    private readonly List<Part> _parts = new();

    private readonly Dictionary<WeaponId, float> _wcd = new();
    private readonly Dictionary<Mob, float> _auraHit = new();
    private float _whipBackAt = -1f;
    private float _whipBackPct;
    private float _auraCd;
    private int _comboShown;

    private void SimReset()
    {
        _pshots.Clear(); _nums.Clear(); _parts.Clear();
        _wcd.Clear(); _auraHit.Clear();
        _pendingCards = 0; _cards.Clear();
        _whipBackAt = -1f; _comboShown = 0; _toastUntil = 0;
        _evolved.Clear();
    }

    private void WeaponsTick(float dt)
    {
        int blvl = _loadout.WeaponLevels[(int)WeaponId.Blades];
        _bladeStats = WeaponsCatalog.Stats(WeaponId.Blades, blvl);
        if (_evolved.Contains(WeaponId.Blades))
            _bladeStats = new WeaponLevelStats { Damage = 26f, Count = 6, Radius = 1.6f, SpeedDeg = 320f };
        _bladesN = _bladeStats.Count;
        _bladeR = _bladeStats.Radius * AreaMult;
        _bladeAngle = (_bladeAngle + _bladeStats.SpeedDeg * dt) % 360f;
        if (_evolved.Contains(WeaponId.Blades)) _bladesOn = true;
        else if (blvl >= 4)
        {
            _bladeUptime += dt;
            float cycle = _bladeStats.UptimeSec + _bladeStats.Extra;
            _bladesOn = _bladeUptime % cycle < _bladeStats.UptimeSec;
        }
        else _bladesOn = true;

        _magnetR = Kinematics.MagnetRadius(ScaleNow, PassLvl(PassiveId.Magnet), MetaSteps(5));

        TickDaggers(dt);
        TickAxe(dt);
        TickLightning(dt);
        TickAura(dt);
        TickWhip(dt);
        TickPlayerShots(dt);
    }

    private void RegenTick(float dt)
    {
        if (_hp > 0f && RegenPerSec > 0f)
            _hp = MathF.Min(_maxHp, _hp + RegenPerSec * dt);
    }

    private Mob? NearestMob(float radius)
    {
        Mob? best = null;
        float bd = radius;
        foreach (Mob m in _mobs)
        {
            if (m.Hp <= 0) continue;
            float dx = m.X - _px, dy = m.Y - _py, d = MathF.Sqrt(dx * dx + dy * dy);
            if (d < bd) { bd = d; best = m; }
        }
        return best;
    }

    private (float X, float Y) Norm(float dx, float dy)
    {
        float l = MathF.Max(MathF.Sqrt(dx * dx + dy * dy), 1e-4f);
        return (dx / l, dy / l);
    }

    private void TickDaggers(float dt)
    {
        int lvl = _loadout.WeaponLevels[(int)WeaponId.Daggers];
        if (lvl == 0) return;
        WeaponLevelStats s = WeaponsCatalog.Stats(WeaponId.Daggers, lvl);
        float spread = 8f;
        if (_evolved.Contains(WeaponId.Daggers)) { s = new WeaponLevelStats { Damage = 12f, Count = 6, CooldownSec = 0.70f, Extra = 3f }; spread = 12f; }
        _wcd.TryGetValue(WeaponId.Daggers, out float cd);
        cd -= dt;
        if (cd > 0) { _wcd[WeaponId.Daggers] = cd; return; }
        Mob? target = NearestMob(WeaponsCatalog.DaggerTargetRadius);
        if (target == null) return;
        _wcd[WeaponId.Daggers] = s.CooldownSec * CooldownMult;
        (float nx, float ny) = Norm(target.X - _px, target.Y - _py);
        for (int i = 0; i < s.Count; i++)
        {
            float sp = (i - (s.Count - 1) * 0.5f) * spread * MathF.PI / 180f;
            float vx = nx * MathF.Cos(sp) - ny * MathF.Sin(sp);
            float vy = nx * MathF.Sin(sp) + ny * MathF.Cos(sp);
            _pshots.Add(new PBullet
            {
                X = _px, Y = _py,
                Vx = vx * WeaponsCatalog.ProjectileSpeedDagger,
                Vy = vy * WeaponsCatalog.ProjectileSpeedDagger,
                Life = WeaponsCatalog.DaggerLifeSec,
                Damage = s.Damage * PowerMult,
                Pierce = (int)s.Extra
            });
        }
    }

    private void TickAxe(float dt)
    {
        int lvl = _loadout.WeaponLevels[(int)WeaponId.Axe];
        if (lvl == 0) return;
        WeaponLevelStats s = WeaponsCatalog.Stats(WeaponId.Axe, lvl);
        if (_evolved.Contains(WeaponId.Axe)) s = new WeaponLevelStats { Damage = 40f, Count = 6, CooldownSec = s.CooldownSec };
        _wcd.TryGetValue(WeaponId.Axe, out float cd);
        cd -= dt / CooldownMult;
        if (cd > 0) { _wcd[WeaponId.Axe] = cd; return; }
        cd = s.CooldownSec; _wcd[WeaponId.Axe] = cd;
        for (int i = 0; i < s.Count; i++)
        {
            float fan = ((float)_rng.NextDouble() * 2f - 1f) * WeaponsCatalog.AxeFanDegrees * MathF.PI / 180f;
            _pshots.Add(new PBullet
            {
                X = _px, Y = _py,
                Vx = MathF.Sin(fan) * 3.5f, Vy = 7f,
                Life = 1.6f,
                Damage = s.Damage * PowerMult,
                Arc = true
            });
        }
    }

    private void TickLightning(float dt)
    {
        int lvl = _loadout.WeaponLevels[(int)WeaponId.Lightning];
        if (lvl == 0) return;
        WeaponLevelStats s = WeaponsCatalog.Stats(WeaponId.Lightning, lvl);
        bool chain = _evolved.Contains(WeaponId.Lightning);
        if (chain) s = new WeaponLevelStats { Damage = s.Damage, Count = 4, CooldownSec = 1.0f, Radius = s.Radius };
        _wcd.TryGetValue(WeaponId.Lightning, out float cd);
        cd -= dt / CooldownMult;
        if (cd > 0) { _wcd[WeaponId.Lightning] = cd; return; }
        List<Mob> pool = _mobs.Where(m => m.Hp > 0 &&
            MathF.Pow(m.X - _px, 2) + MathF.Pow(m.Y - _py, 2) < 144f).ToList();
        if (pool.Count == 0) return;
        cd = s.CooldownSec; _wcd[WeaponId.Lightning] = cd;
        int strikes = s.Count;
        while (strikes-- > 0 && pool.Count > 0)
        {
            Mob t = pool[_rng.Next(pool.Count)];
            float r = s.Radius * AreaMult;
            foreach (Mob m in _mobs)
            {
                if (m.Hp <= 0) continue;
                float dx = m.X - t.X, dy = m.Y - t.Y;
                if (dx * dx + dy * dy <= r * r) HurtMob(m, s.Damage * PowerMult);
            }
            SpawnBolt(t.X, t.Y);
            if (chain)
            {
                float fall = 0.6f;
                var near = pool.OrderBy(o => MathF.Pow(o.X - t.X, 2) + MathF.Pow(o.Y - t.Y, 2)).Take(2).ToList();
                foreach (var cn in near)
                {
                    HurtMob(cn, s.Damage * PowerMult * fall);
                    SpawnBolt(cn.X, cn.Y);
                    fall *= 0.6f;
                    pool.Remove(cn);
                }
            }
            pool.Remove(t);
        }
    }

    private void TickAura(float dt)
    {
        int lvl = _loadout.WeaponLevels[(int)WeaponId.Aura];
        if (lvl == 0) return;
        WeaponLevelStats s = WeaponsCatalog.Stats(WeaponId.Aura, lvl);
        bool void_ = _evolved.Contains(WeaponId.Aura);
        if (void_) s = new WeaponLevelStats { Damage = 14f, Radius = 3.0f, Extra = 0.25f };
        _auraCd -= dt;
        if (_auraCd > 0) return;
        _auraCd = void_ ? 0.4f : WeaponsCatalog.Aura_TickSec;
        float r = s.Radius * AreaMult;
        foreach (Mob m in _mobs)
        {
            if (m.Hp <= 0) continue;
            float dx = m.X - _px, dy = m.Y - _py;
            if (dx * dx + dy * dy > r * r) continue;
            float next = _auraHit.TryGetValue(m, out float t) ? t : 0f;
            if (_runTime < next) continue;
            _auraHit[m] = _runTime + (void_ ? 0.75f : WeaponsCatalog.Aura_RehitSec);
            HurtMob(m, s.Damage * PowerMult);
            if (void_ || lvl >= 5) m.SlowUntil = _runTime + 0.6f;
            if (void_)
            {
                (float px2, float py2) = Norm(_px - m.X, _py - m.Y);
                m.X += px2 * 0.8f * WeaponsCatalog.Aura_TickSec;
                m.Y += py2 * 0.8f * WeaponsCatalog.Aura_TickSec;
            }
        }
    }

    private void TickWhip(float dt)
    {
        int lvl = _loadout.WeaponLevels[(int)WeaponId.Whip];
        if (lvl == 0) return;
        WeaponLevelStats s = WeaponsCatalog.Stats(WeaponId.Whip, lvl);
        bool circle = _evolved.Contains(WeaponId.Whip);
        _wcd.TryGetValue(WeaponId.Whip, out float cd);
        cd -= dt / CooldownMult;
        if (_whipBackAt > 0 && _runTime >= _whipBackAt)
        {
            WhipSlash(-_dirX, -_dirY, s, _whipBackPct);
            _whipBackAt = -1;
        }
        if (cd > 0) { _wcd[WeaponId.Whip] = cd; return; }
        cd = s.CooldownSec; _wcd[WeaponId.Whip] = cd;
        if (circle)
        {
            float vamp = 0f;
            foreach (Mob m in _mobs)
            {
                if (m.Hp <= 0) continue;
                float ddx = m.X - _px, ddy = m.Y - _py;
                if (ddx * ddx + ddy * ddy > 2.8f * 2.8f * AreaMult * AreaMult) continue;
                float before = m.Hp;
                HurtMob(m, 30f * PowerMult);
                vamp += MathF.Max(0f, before - MathF.Max(0f, m.Hp)) * 0.05f;
            }
            if (vamp > 0f) _hp = MathF.Min(_maxHp, _hp + vamp);
            return;
        }
        WhipSlash(_dirX, _dirY, s, 1f);
        if (WeaponsCatalog.HasBackswing(WeaponId.Whip, lvl))
        {
            _whipBackAt = _runTime + WeaponsCatalog.Whip_HitVisualSec;
            _whipBackPct = WeaponsCatalog.BackswingDamagePct(WeaponId.Whip, lvl);
        }
    }

    private void WhipSlash(float dx, float dy, WeaponLevelStats s, float pct)
    {
        float len = s.Radius * AreaMult, wid = MathF.Max(0.5f, s.Extra) * AreaMult;
        float ang = MathF.Atan2(dy, dx);
        float ca = MathF.Cos(-ang), sa = MathF.Sin(-ang);
        float mx = (dx) * len * 0.5f, my = (dy) * len * 0.5f;
        foreach (Mob m in _mobs)
        {
            if (m.Hp <= 0) continue;
            float rx = m.X - (_px + mx), ry = m.Y - (_py + my);
            float lx = rx * ca - ry * sa, ly = rx * sa + ry * ca;
            if (MathF.Abs(lx) <= len / 2f + 0.3f && MathF.Abs(ly) <= wid / 2f + 0.3f)
                HurtMob(m, s.Damage * pct * PowerMult);
        }
    }

    private void TickPlayerShots(float dt)
    {
        for (int i = _pshots.Count - 1; i >= 0; i--)
        {
            PBullet b = _pshots[i];
            b.Life -= dt;
            if (b.Arc) b.Vy += 18f * dt;
            b.X += b.Vx * dt;
            b.Y += b.Vy * dt;
            bool dead = b.Life <= 0;
            foreach (Mob m in _mobs)
            {
                if (m.Hp <= 0 || b.Hit.Contains(m)) continue;
                float rr = 0.3f + 0.35f * m.Size;
                float dx = m.X - b.X, dy = m.Y - b.Y;
                if (dx * dx + dy * dy > rr * rr) continue;
                b.Hit.Add(m);
                HurtMob(m, b.Damage);
                if (b.Pierce > 0) b.Pierce--;
                else { dead = true; break; }
            }
            if (dead) _pshots.RemoveAt(i);
        }
    }

    private void SpawnDmg(float x, float y, float val, bool crit = false)
    {
        if (_nums.Count > 40) _nums.RemoveAt(0);
        _nums.Add(new DmgNum { X = x, Y = y, Txt = val.ToString("0"), Life = 0.6f, Crit = crit });
    }

    private void SpawnBolt(float x, float y)
    {
        for (int i = 0; i < 6; i++)
            _parts.Add(new Part { X = x, Y = y, Vx = (float)(_rng.NextDouble() - .5) * 6, Vy = (float)(_rng.NextDouble() - .5) * 6, Life = 0.25f, R = 255, G = 255, B = 180 });
    }

    private void DeathBurst(Mob m)
    {
        var col = KindRgb(m.Kind);
        for (int i = 0; i < 10; i++)
        {
            float a = (float)_rng.NextDouble() * MathF.PI * 2f;
            float sp = 2f + (float)_rng.NextDouble() * 3f;
            _parts.Add(new Part { X = m.X, Y = m.Y, Vx = MathF.Cos(a) * sp, Vy = MathF.Sin(a) * sp, Life = 0.4f, R = col.r, G = col.g, B = col.b });
        }
        if (_kills >= (_comboShown + 1) * 50)
        {
            _comboShown = _kills / 50;
            _comboUntil = _runTime + 1.2f;
        }
    }

    private float _comboUntil;
    internal readonly HashSet<WeaponId> _evolved = new();

    private (byte r, byte g, byte b) KindRgb(EnemyKind k) => k switch
    {
        EnemyKind.Walker => (106, 153, 78),
        EnemyKind.Runner => (255, 183, 3),
        EnemyKind.Tank => (231, 111, 81),
        EnemyKind.Shooter => (199, 125, 255),
        EnemyKind.Sprinter => (144, 190, 109),
        _ => (249, 65, 68)
    };

    private void JuiceTick(float dt)
    {
        for (int i = _nums.Count - 1; i >= 0; i--)
        {
            DmgNum n = _nums[i];
            n.Life -= dt; n.Y -= 0.9f * dt;
            if (n.Life <= 0) _nums.RemoveAt(i);
        }
        for (int i = _parts.Count - 1; i >= 0; i--)
        {
            Part q = _parts[i];
            q.Life -= dt; q.Vy += 5f * dt;
            q.X += q.Vx * dt; q.Y += q.Vy * dt;
            if (q.Life <= 0) _parts.RemoveAt(i);
        }
    }

    private void ProcessLevelUps()
    {
        if (_pendingCards > 0 && _phase == Phase.Run) OpenLevelUpCards();
    }

    private void OpenLevelUpCards()
    {
        _cards.Clear();
        _cards.AddRange(UpgradeDeck.OfferThree(_loadout, _rng, 0x3F));
        if (_cards.Count == 0) { _pendingCards = 0; return; }
        _phase = Phase.LevelUp;
        Play("levelup", 0.8f);
        _joyActive = false; _jdx = _jdy = 0;
        for (int i = 0; i < 3; i++)
        {
            float w = Width * 0.29f, h = Height * 0.26f;
            float x = Width * (0.06f + 0.315f * i);
            float y = Height * 0.36f;
            _cardRects[i] = new RectF(x, y, x + w, y + h);
        }
    }

    private void ChooseCard(int i)
    {
        if (i < 0 || i >= _cards.Count) return;
        ApplyCard(_cards[i]);
        _cards.Clear();
        _pendingCards--;
        if (_pendingCards > 0) OpenLevelUpCards();
        else if (_phase == Phase.LevelUp) _phase = Phase.Run;
    }

    private void ApplyCard(UpgradeCard card)
    {
        switch (card.Type)
        {
            case CardType.NewWeapon:
                _loadout.WeaponLevels[(int)card.Weapon] = 1;
                _loadout.WeaponCount++;
                _wcd[card.Weapon] = 0;
                break;
            case CardType.UpgradeWeapon:
                int cur = _loadout.WeaponLevels[(int)card.Weapon];
                _loadout.WeaponLevels[(int)card.Weapon] = Math.Min(WeaponsCatalog.MaxLevel, cur + 1);
                break;
            case CardType.Passive:
                _loadout.PassiveLevels[(int)card.Passive]++;
                if (_loadout.PassiveLevels[(int)card.Passive] == 1) _loadout.PassiveCount++;
                if (card.Passive == PassiveId.Vitality) _hp = MathF.Min(MaxHpNow, _hp + GameBalance.Player_MaxHP * 0.10f);
                break;
        }
    }

    private void OpenSmallChest()
    {
        List<UpgradeCard> cards = UpgradeDeck.OfferThree(_loadout, _rng, 0x3F);
        if (cards.Count == 0) return;
        ApplyCard(cards[0]);
        Play("chest", 0.8f);
        Toast("СУНДУК: " + CardLabel(cards[0]));
    }

    private bool TryEvolveWeapon()
    {
        for (int w = 0; w < 6; w++)
        {
            var id = (WeaponId)w;
            if (_loadout.WeaponLevels[w] != WeaponsCatalog.MaxLevel) continue;
            if (!Evolutions.TryFind(id, out Evolutions.Recipe recipe)) continue;
            if (PassLvl(recipe.RequiredPassive) < GameBalance.Evo_ReqPassiveLvl) continue;
            _evolved.Add(id);
            Play("chest", 1f);
            Toast("ЭВОЛЮЦИЯ: " + recipe.ResultName + "!");
            return true;
        }
        return false;
    }

    private void OpenBigChest()
    {
        Play("chest", 0.9f);
        if (TryEvolveWeapon()) return;
        List<UpgradeCard> cards = UpgradeDeck.OfferThree(_loadout, _rng, 0x3F);
        int take = Math.Min(2, cards.Count);
        for (int i = 0; i < take; i++) ApplyCard(cards[i]);
        var labels = cards.Take(take).Select(CardLabel);
        Toast("БОЛЬШОЙ СУНДУК: " + string.Join(" + ", labels));
    }

    private void Toast(string text)
    {
        _toast = text;
        _toastUntil = _runTime + 2.5f;
    }

    internal static string CardLabel(UpgradeCard c)
    {
        if (c.Type == CardType.Passive) return PassiveRu[(int)c.Passive];
        string w = WeaponsCatalog.Names[(int)c.Weapon];
        return c.Type == CardType.NewWeapon ? w : w + " ур.+1";
    }

    internal static readonly string[] PassiveRu =
        { "Магнит", "Скорость", "Прочность", "Реген", "Кулдаун −8%", "Площадь +10%", "Броня +1", "Опыт +10%", "Мощь +10%" };
}
