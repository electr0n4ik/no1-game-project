using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Context = Android.Content.Context;
using Android.Views;
using Rubilovo.Logic;
using System.Linq;

namespace Rubilovo.Android;

[Application(HardwareAccelerated = false)]
public class App : Application
{
    protected App(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership) { }
}

[Activity(
    Label = "Рубилово 0.1",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(new GameView(this));
    }
}

public class GameView : SurfaceView, ISurfaceHolderCallback
{
    private enum Phase { Run, Dead, Victory }

    private sealed class Mob
    {
        public float ShooterCd;
        public EnemyKind Kind;
        public float X, Y, Hp, Speed, Damage, Size = 1f;
        public bool Elite, Boss, FinalBoss, Rewardless;
    }

    private sealed class Shot { public float X, Y, Vx, Vy, Life, Damage; }
    private sealed class Orb { public float X, Y, Value; }
    private sealed class BladeTick { public Mob Mob; public float NextAt; }

    private readonly ISurfaceHolder _holder;
    private Thread? _loop;
    private volatile bool _running;
    private readonly System.Random _rng = new();

    private readonly List<Mob> _mobs = new();
    private readonly List<Shot> _shots = new();
    private readonly List<Orb> _orbs = new();
    private readonly List<BladeTick> _bladeTicks = new();
    private readonly Dictionary<Mob, float> _touchCd = new();

    private float _px, _py, _hp = GameBalance.Player_MaxHP;
    private int _level = 1, _levelUps;
    private double _xp;
    private float _bladeAngle, _runTime, _lastHurt, _eliteTimer;
    private int _spawnNumber, _kills, _elites, _bossesKilled, _eliteDropIndex;
    private readonly bool[] _bossSpawned = new bool[4];
    private Mob? _bossAlive;
    private Phase _phase = Phase.Run;
    private long _endedAt;

    private bool _joyActive;
    private float _joyOx, _joyOy, _jdx, _jdy;
    private float _density = 2f;
    private long _nullLocks, _posted;

    private const float ArenaHalf = 20f;
    private const float ArenaClamp = 19.2f;

    private readonly Paint _p = new() { AntiAlias = true };

    public GameView(Context context) : base(context)
    {
        Holder.AddCallback(this);
        KeepScreenOn = true;
        Focusable = true;
        FocusableInTouchMode = true;
    }

    public void SurfaceCreated(ISurfaceHolder holder)
    {
        global::Android.Util.Log.Info("Game", "SurfaceCreated");
        ResumeLoop();
    }
    public void SurfaceChanged(ISurfaceHolder holder, Format format, int w, int h)
        => global::Android.Util.Log.Info("Game", $"SurfaceChanged {w}x{h}");
    public void SurfaceDestroyed(ISurfaceHolder holder) => PauseLoop();

    public void ResumeLoop()
    {
        if (_running) return;
        _running = true;
        _density = Resources!.DisplayMetrics.Density;
        ResetRun();
        new Thread(Loop) { IsBackground = true }.Start();
    }

    public void PauseLoop() => _running = false;

    private void Loop()
    {
        long last = Java.Lang.JavaSystem.CurrentTimeMillis();
        while (_running)
        {
            long now = Java.Lang.JavaSystem.CurrentTimeMillis();
            float dt = Math.Min((now - last) / 1000f, 0.05f);
            last = now;
            try
            {
                if (_phase == Phase.Run && Width > 0) Update(dt);
                Canvas? c = Holder.LockCanvas();
                if (c == null)
                {
                    _nullLocks++;
                    if (_nullLocks % 120 == 1)
                        global::Android.Util.Log.Info("Game", $"LockCanvas null x{_nullLocks}");
                    System.Threading.Thread.Sleep(16);
                    continue;
                }
                OnDraw(c);
                Holder.UnlockCanvasAndPost(c);
                _posted++;
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("Game", ex.ToString());
                _running = false;
            }
            if (_posted % 300 == 1)
                global::Android.Util.Log.Info("Game", $"posted={_posted}");
            System.Threading.Thread.Sleep(8);
        }
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e == null) return false;
        switch (e.Action)
        {
            case MotionEventActions.Down:
                if (_phase != Phase.Run && Java.Lang.JavaSystem.CurrentTimeMillis() - _endedAt > 800)
                {
                    ResetRun();
                    break;
                }
                if (e.GetY() < Height * 0.7f)
                {
                    _joyActive = true;
                    _joyOx = e.GetX(); _joyOy = e.GetY(); _jdx = _jdy = 0;
                }
                break;

            case MotionEventActions.Move when _joyActive:
            {
                float dx = e.GetX() - _joyOx, dy = e.GetY() - _joyOy;
                float maxPx = GameBalance.Input_MaxPixels * _density;
                float deadPx = GameBalance.Input_DeadzonePx * _density;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d <= deadPx) { _jdx = _jdy = 0; break; }
                float mag = d >= maxPx ? 1f : (d - deadPx) / (maxPx - deadPx);
                _jdx = dx / d * mag; _jdy = dy / d * mag;
                break;
            }

            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                _joyActive = false; _jdx = _jdy = 0;
                break;
        }
        return true;
    }

    private void ResetRun()
    {
        _px = _py = 0; _hp = GameBalance.Player_MaxHP;
        _level = 1; _levelUps = 0; _xp = 0;
        _mobs.Clear(); _shots.Clear(); _orbs.Clear(); _bladeTicks.Clear(); _touchCd.Clear();
        _spawnNumber = 0; _kills = _elites = _bossesKilled = 0; _eliteDropIndex = 0;
        Array.Clear(_bossSpawned);
        _bossAlive = null;
        _runTime = 0; _bladeAngle = 0;
        _phase = Phase.Run;
    }

    private float ScaleNow => Kinematics.ScaleAfterLevelUps(_levelUps);
    private int BladesN => 2 + _levelUps / 3;
    private float BladeR => 1.1f * ScaleNow;
    private float MagnetR => Kinematics.MagnetRadius(ScaleNow, 0, 0);
    private float OrthoNow => Kinematics.OrthoTarget(ScaleNow);

    private void Update(float dt)
    {
        _runTime += dt;
        float minutes = _runTime / 60f;

        float mag = MathF.Min(MathF.Sqrt(_jdx * _jdx + _jdy * _jdy), 1f);
        float speed = Kinematics.FinalSpeed(mag, ScaleNow, 0, 0);
        float l = MathF.Max(MathF.Sqrt(_jdx * _jdx + _jdy * _jdy), 1e-5f);
        _px = Math.Clamp(_px + _jdx / l * speed * dt, -ArenaClamp, ArenaClamp);
        _py = Math.Clamp(_py + _jdy / l * speed * dt, -ArenaClamp, ArenaClamp);

        _bladeAngle = (_bladeAngle + 260f * dt) % 360f;
        Blades(minutes);

        Director(dt, minutes);
        Mobs(dt);
        for (int i = _shots.Count - 1; i >= 0; i--)
        {
            Shot s = _shots[i];
            s.Life -= dt; s.X += s.Vx * dt; s.Y += s.Vy * dt;
            float dx = s.X - _px, dy = s.Y - _py;
            bool hit = dx * dx + dy * dy < MathF.Pow(0.3f * ScaleNow + 0.15f, 2);
            if (hit) Hurt(s.Damage);
            if (s.Life <= 0 || hit) _shots.RemoveAt(i);
        }
        for (int i = _orbs.Count - 1; i >= 0; i--)
        {
            Orb o = _orbs[i];
            float dx = _px - o.X, dy = _py - o.Y, d = MathF.Sqrt(dx * dx + dy * dy);
            if (d < MagnetR) { o.X += dx / MathF.Max(d, .01f) * 10f * dt; o.Y += dy / MathF.Max(d, .01f) * 10f * dt; }
            if (d < 0.35f * ScaleNow + 0.25f) { AddXp(o.Value); _orbs.RemoveAt(i); }
        }
    }

    private void Blades(float minutes)
    {
        float dmg = 10f * (1f + _levelUps * 0.06f);
        for (int b = 0; b < BladesN; b++)
        {
            float a = (_bladeAngle + 360f / BladesN * b) * MathF.PI / 180f;
            float bx = _px + MathF.Cos(a) * BladeR, by = _py + MathF.Sin(a) * BladeR;
            foreach (Mob m in _mobs)
            {
                if (m.Hp <= 0) continue;
                float hr = 0.35f + 0.4f * m.Size;
                float dx = m.X - bx, dy = m.Y - by;
                if (dx * dx + dy * dy > hr * hr) continue;
                BladeTick t = _bladeTicks.FirstOrDefault(x => ReferenceEquals(x.Mob, m))!;
                if (t is null) { t = new BladeTick { Mob = m }; _bladeTicks.Add(t); }
                if (_runTime < t.NextAt) continue;
                t.NextAt = _runTime + WeaponsCatalog.Blades_RehitSec;
                HurtMob(m, dmg);
            }
        }
        _bladeTicks.RemoveAll(t => t.Mob.Hp <= 0 || !_mobs.Contains(t.Mob));
    }

    private void Director(float dt, float minutes)
    {
        for (int i = 0; i < WaveScript.BossScheduleMinutes.Length; i++)
        {
            if (_bossSpawned[i] || minutes < WaveScript.BossScheduleMinutes[i]) continue;
            _bossSpawned[i] = true;
            BossStats st = WaveScript.BossByIndex(i);
            var b = new Mob { Kind = EnemyKind.Tank, Boss = true, FinalBoss = i == 3,
                X = RingX(), Y = RingY(), Hp = st.Hp, Speed = st.Speed, Damage = st.ContactDamage, Size = 2.2f };
            _mobs.Add(b);
            _bossAlive = b;
        }

        bool bossFight = _bossAlive is { Hp: > 0 };

        if (!bossFight && minutes >= WaveScript.WaveConstants.Elite_FromMinute)
        {
            _eliteTimer -= dt;
            if (_eliteTimer <= 0)
            {
                EliteSpawn(minutes);
                _eliteTimer = WaveScript.WaveConstants.Elite_TimerMinSec +
                    (float)_rng.NextDouble() * (WaveScript.WaveConstants.Elite_TimerMaxSec -
                                                WaveScript.WaveConstants.Elite_TimerMinSec);
            }
        }
        if (bossFight) return;

        int cap = WaveScript.AliveCap(minutes);
        int alive = _mobs.Count(m => m.Hp > 0);
        if (alive >= cap) return;

        _spawnNumber++;
        int batch = Math.Min(WaveScript.BatchSize(minutes), cap - alive);
        for (int i = 0; i < batch; i++)
            Spawn(WaveScript.RollKind(minutes, _rng.NextDouble()), minutes, false);
    }

    private void Spawn(EnemyKind kind, float minutes, bool elite)
    {
        EnemyBaseStats b = WaveScript.Base(kind);
        _mobs.Add(new Mob
        {
            Kind = kind,
            X = RingX(), Y = RingY(),
            Hp = WaveScript.ScaledHp(minutes, kind) * (elite ? WaveScript.WaveConstants.Elite_HpMult : 1),
            Speed = b.Speed * (elite ? WaveScript.WaveConstants.Elite_SpeedMult : 1),
            Damage = WaveScript.ScaledDamage(minutes, kind) * (elite ? WaveScript.WaveConstants.Elite_DamageMult : 1),
            Size = elite ? WaveScript.WaveConstants.Elite_SizeMult : 1f,
            Elite = elite
        });
    }

    private void EliteSpawn(float minutes)
    {
        Mob? pick = _mobs.FirstOrDefault(m => m.Hp > 0 && !m.Boss && !m.Elite);
        if (pick != null)
        {
            pick.Elite = true;
            pick.Hp *= 6; pick.Damage *= 1.5f; pick.Size *= 1.4f; pick.Speed *= 0.9f;
            return;
        }
        Spawn((EnemyKind)_rng.Next(6), minutes, true);
    }

    private float RingX()
    {
        float halfW = OrthoNow * Aspect + GameBalance.Spawn_RingExtra;
        double a = _rng.NextDouble() * Math.PI * 2;
        return Math.Clamp(_px + MathF.Cos((float)a) * halfW, -ArenaClamp, ArenaClamp);
    }

    private float RingY()
    {
        float halfH = OrthoNow + GameBalance.Spawn_RingExtra;
        double a = _rng.NextDouble() * Math.PI * 2;
        return Math.Clamp(_py + MathF.Sin((float)a) * halfH, -ArenaClamp, ArenaClamp);
    }

    private float Aspect => Width / (float)Math.Max(1, Height);

    private void Mobs(float dt)
    {
        for (int i = _mobs.Count - 1; i >= 0; i--)
        {
            Mob m = _mobs[i];
            if (m.Hp <= 0) { _mobs.RemoveAt(i); continue; }

            float dx = _px - m.X, dy = _py - m.Y, d = MathF.Sqrt(dx * dx + dy * dy);
            if (d > 0.01f) { m.X += dx / d * m.Speed * dt; m.Y += dy / d * m.Speed * dt; }

            if (m.Kind == EnemyKind.Shooter)
            {
                m.ShooterCd -= dt;
                if (d is > 3.5f and < 6.5f && m.ShooterCd <= 0)
                {
                    m.ShooterCd = WaveScript.WaveConstants.Shooter_FireCooldown;
                    _shots.Add(new Shot { X = m.X, Y = m.Y,
                        Vx = dx / d * WaveScript.WaveConstants.Shooter_ProjectileSpeed,
                        Vy = dy / d * WaveScript.WaveConstants.Shooter_ProjectileSpeed,
                        Life = WaveScript.WaveConstants.Shooter_ProjectileLife, Damage = m.Damage });
                }
            }

            if (m.Kind == EnemyKind.Kamikaze && d < 1.1f + 0.3f * m.Size)
            {
                Hurt(m.Damage);
                Kill(m, rewards: false);
                continue;
            }

            float touch = 0.45f * ScaleNow + 0.4f * m.Size;
            if (d < touch && _runTime - _touchCd.GetValueOrDefault(m, -9f) >= GameBalance.Contact_HitCooldownEnemy)
            {
                _touchCd[m] = _runTime;
                Hurt(m.Damage);
            }
        }
    }

    private void Hurt(float raw)
    {
        _hp = Math.Max(0f, _hp - Math.Max(1f, raw));
        if (_phase == Phase.Run) _lastHurt = _runTime;
    }

    private void HurtMob(Mob m, float dmg)
    {
        m.Hp -= dmg;
        if (m.Hp > 0) return;
        _kills++;
        if (m.Elite) _elites++;
        if (m.Boss)
        {
            _bossesKilled++;
            _orbs.Add(new Orb { X = m.X, Y = m.Y, Value = GameBalance.Boss_XP });
            if (ReferenceEquals(_bossAlive, m)) _bossAlive = null;
            m.Hp = 0;
            if (m.FinalBoss) { _phase = Phase.Victory; _endedAt = Java.Lang.JavaSystem.CurrentTimeMillis(); }
            return;
        }
        if (m.Elite)
        {
            if (_eliteDropIndex % 2 == 0)
                _orbs.Add(new Orb { X = m.X, Y = m.Y, Value = GameBalance.Elite_BombXP });
            else
                _xp = XpCurve.Need(_level); // малый сундук ≈ мгновенный уровень
            _eliteDropIndex++;
        }
        else
        {
            int[] xp = { 1, 1, 3, 2, 1, 1 };
            _orbs.Add(new Orb { X = m.X, Y = m.Y, Value = xp[(int)m.Kind] });
        }
        m.Hp = 0;
    }

    private void Kill(Mob m, bool rewards) { if (!rewards) m.Hp = 0; }

    private void AddXp(double v)
    {
        _xp += v;
        while (_xp >= XpCurve.Need(_level))
        {
            _xp -= XpCurve.Need(_level);
            _level++; _levelUps++;
        }
    }

    protected override void OnDraw(Canvas c)
    {
        c.DrawRGB(43, 45, 66);
        float ppu = Height / (OrthoNow * 2f);
        float cx = Width / 2f, cy = Height / 2f;
        float X(float wx) => cx + (wx - _px) * ppu;
        float Y(float wy) => cy + (wy - _py) * ppu;

        _p.SetStyle(Paint.Style.Stroke);
        _p.StrokeWidth = 2;
        _p.Color = Color.Rgb(61, 64, 96);
        c.DrawRect(X(-ArenaHalf), Y(-ArenaHalf), X(ArenaHalf), Y(ArenaHalf), _p);

        _p.SetStyle(Paint.Style.Fill);
        _p.Color = Color.Rgb(255, 209, 102);
        foreach (Orb o in _orbs)
            c.DrawCircle(X(o.X), Y(o.Y), MathF.Max(3f, 0.14f * ppu), _p);

        foreach (Mob m in _mobs)
        {
            if (m.Hp <= 0) continue;
            float rr = 0.42f * m.Size * ppu;
            _p.SetStyle(Paint.Style.Fill);
            _p.Color = MobColor(m);
            c.DrawCircle(X(m.X), Y(m.Y), rr, _p);
            if (m.Elite || m.Boss)
            {
                _p.SetStyle(Paint.Style.Stroke);
                _p.StrokeWidth = 3;
                _p.Color = Color.Rgb(239, 71, 111);
                c.DrawCircle(X(m.X), Y(m.Y), rr + 5, _p);
            }
        }

        if (_phase == Phase.Run)
        {
            _p.SetStyle(Paint.Style.Fill);
            _p.Color = Color.Rgb(255, 209, 102);
            for (int b = 0; b < BladesN; b++)
            {
                float a = (_bladeAngle + 360f / BladesN * b) * MathF.PI / 180f;
                c.DrawCircle(X(_px + MathF.Cos(a) * BladeR), Y(_py + MathF.Sin(a) * BladeR),
                    MathF.Max(4f, 0.18f * ppu), _p);
            }
            _p.Color = _runTime - _lastHurt < 0.12f ? Color.Rgb(255, 120, 120) : Color.White;
            c.DrawCircle(cx, cy, 0.45f * ScaleNow * ppu, _p);
        }

        if (_joyActive)
        {
            _p.SetStyle(Paint.Style.Stroke);
            _p.StrokeWidth = 2;
            _p.Color = Color.Argb(90, 255, 255, 255);
            c.DrawCircle(_joyOx, _joyOy, GameBalance.Input_MaxPixels * _density, _p);
        }

        _p.SetStyle(Paint.Style.Fill);
        _p.Color = Color.White;
        _p.TextSize = 15 * _density;
        int mm = (int)(_runTime / 60), ss = (int)(_runTime % 60);
        string meat = Economy.MeatForRunFinal(_kills, _elites, _bossesKilled, 1).ToString();
        c.DrawText($"{mm}:{ss:00}  LVL{_level}  K{_kills} E{_elites} B{_bossesKilled}  ~{meat}kg  HP {_hp:0}",
            12 * _density, 24 * _density, _p);
        _p.Color = Color.Rgb(6, 214, 160);
        c.DrawRect(0, 0, Width * Math.Clamp(_hp / GameBalance.Player_MaxHP, 0f, 1f), 5 * _density, _p);

        if (_phase != Phase.Run)
        {
            _p.Color = Color.Argb(175, 20, 21, 38);
            c.DrawRect(0, 0, Width, Height, _p);
            _p.Color = Color.White;
            _p.TextSize = 32 * _density;
            string title = _phase == Phase.Victory ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
            c.DrawText(title, cx - _p.MeasureText(title) / 2, cy - 40 * _density, _p);
            _p.TextSize = 18 * _density;
            string sub = $"время {mm}:{ss:00}   убийства {_kills}   элиты {_elites}   боссы {_bossesKilled}   мясо +{meat}";
            c.DrawText(sub, cx - _p.MeasureText(sub) / 2, cy, _p);
            _p.Color = Color.Rgb(184, 189, 212);
            string hint = "коснитесь экрана — новый забег";
            c.DrawText(hint, cx - _p.MeasureText(hint) / 2, cy + 36 * _density, _p);
        }
    }

    private static Color MobColor(Mob m)
    {
        if (m.Boss) return Color.Rgb(217, 26, 153);
        if (m.Elite) return Color.Rgb(255, 105, 105);
        return m.Kind switch
        {
            EnemyKind.Walker => Color.Rgb(142, 202, 230),
            EnemyKind.Runner => Color.Rgb(255, 183, 3),
            EnemyKind.Tank => Color.Rgb(231, 111, 81),
            EnemyKind.Shooter => Color.Rgb(199, 125, 255),
            EnemyKind.Sprinter => Color.Rgb(144, 190, 109),
            _ => Color.Rgb(249, 65, 68)
        };
    }
}
