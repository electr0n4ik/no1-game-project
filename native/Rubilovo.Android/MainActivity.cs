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
    Label = "Рубилово 0.4",
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

public partial class GameView : TextureView, TextureView.ISurfaceTextureListener
{
    private enum Phase { Menu, Run, LevelUp, Tree, Paused, Dead, Victory }

    private sealed class Mob
    {
        public float ShooterCd;
        public string Sprite = "walker";
        public EnemyKind Kind;
        public float X, Y, Hp, Speed, Damage, Size = 1f;
        public bool Elite, Boss, FinalBoss, Rewardless;
        public float MaxHp, HitAt = -9f, SlowUntil;
        public int BossState;
        public float StateTimer, PatternCd = 3f, TgtX, TgtY, DashVx, DashVy;
        public int Slot;
        public bool SecondDash;
        public int Pattern;
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
    private Phase _phase = Phase.Menu;
    private bool _survival;
    private float _nextSurvBoss = 18f;
    private static float _bestSurv;
    private long _endedAt;

    private bool _joyActive;
    private float _joyOx, _joyOy, _jdx, _jdy;
    private float _density = 2f;
    private float _spawnCd = 0.8f;
    private float _grace;
    private float _hb;
    private long _nullLocks, _posted;

    private const float ArenaHalf = 20f;
    private const float ArenaClamp = 19.2f;

    private readonly Paint _p = new() { AntiAlias = true };
    private readonly Paint _bp = new() { AntiAlias = true, FilterBitmap = true };
    private readonly Dictionary<string, Bitmap?> _art = new();
    private global::Android.Media.SoundPool? _sp;
    private readonly Dictionary<string, int> _snd = new();
    private float _lastHitSnd;
    private float _face;

    public GameView(Context context) : base(context)
    {
        SurfaceTextureListener = this;
        KeepScreenOn = true;
        Focusable = true;
        FocusableInTouchMode = true;
    }

    private Surface? _surface;

    public void OnSurfaceTextureAvailable(SurfaceTexture surface, int w, int h)
    {
        global::Android.Util.Log.Info("Game", "SurfaceAvailable");
        _surface?.Release();
        _surface = new Surface(surface);
        ResumeLoop();
    }

    public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int w, int h) { }

    public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
    {
        PauseLoop();
        _surface?.Release();
        _surface = null;
        return true;
    }

    public void OnSurfaceTextureUpdated(SurfaceTexture surface) { }

    public void ResumeLoop()
    {
        if (_running) return;
        _running = true;
        _density = Resources!.DisplayMetrics.Density;
        LoadArt();
        LoadSfx();
        new Thread(Loop) { IsBackground = true }.Start();
    }

    public void PauseLoop() => _running = false;

    private void LoadArt()
    {
        string[] names = { "player", "sword", "orb", "orb5", "tile", "walker", "runner", "tank",
            "shooter", "sprinter", "kamikaze", "butcher", "foundry", "executioner",
            "overlord", "decor_rock", "decor_grass", "decor_skull" };
        foreach (string n in names)
        {
            int id = Resources!.GetIdentifier(n, "drawable", Context!.PackageName);
            _art[n] = id != 0 ? BitmapFactory.DecodeResource(Resources, id) : null;
        }
    }

    private void LoadSfx()
    {
        try
        {
            _sp = new global::Android.Media.SoundPool.Builder()
                .SetMaxStreams(4)
                .SetAudioAttributes(new global::Android.Media.AudioAttributes.Builder()
                    .SetUsage(global::Android.Media.AudioUsageKind.Game)
                    .SetContentType(global::Android.Media.AudioContentType.Sonification)
                    .Build())
                .Build();
            foreach (string n in new[] { "hit", "kill", "levelup", "chest", "death", "boss", "shoot" })
            {
                int id = Resources!.GetIdentifier(n, "raw", Context!.PackageName);
                if (id != 0) _snd[n] = _sp.Load(Context, id, 1);
            }
        }
        catch { _sp = null; }
    }

    private void Play(string name, float vol = 0.7f)
    {
        if (_sp == null || !_snd.TryGetValue(name, out int id)) return;
        _sp.Play(id, 1f, vol, 1, 0, 1f);
    }

    private static string KindSprite(EnemyKind k) =>
        new[] { "walker", "runner", "tank", "shooter", "sprinter", "kamikaze" }[(int)k];

    private static readonly float[] KindSizeU = { 0.9f, 1.1f, 1.6f, 0.9f, 0.75f, 0.75f };

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
                Surface? sf = _surface;
                if (sf == null) { System.Threading.Thread.Sleep(16); continue; }
                Canvas? c = sf.LockHardwareCanvas();
                if (c == null) { System.Threading.Thread.Sleep(16); continue; }
                Render(c);
                sf.UnlockCanvasAndPost(c);
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
                if (_phase == Phase.LevelUp)
                {
                    for (int i = 0; i < _cards.Count && i < 3; i++)
                        if (_cardRects[i] != null && InRect(e, _cardRects[i]!)) { ChooseCard(i); break; }
                    break;
                }
                if (_phase == Phase.Tree)
                {
                    HandleTreeTap(e);
                    break;
                }
                if (_phase == Phase.Menu)
                {
                    if (InBtn(e, 7)) { _phase = Phase.Tree; break; }
                    if (InBtn(e, 0)) ResetRun(false);
                    else if (InBtn(e, 1)) ResetRun(true);
                    break;
                }
                if (_phase == Phase.Paused)
                {
                    if (InBtn(e, 4)) _phase = Phase.Run;
                    else if (InBtn(e, 5)) _phase = Phase.Menu;
                    break;
                }
                if (_phase == Phase.Run)
                {
                    if (InBtn(e, 6))
                    {
                        _phase = Phase.Paused;
                        _joyActive = false; _jdx = _jdy = 0;
                        break;
                    }
                    if (e.GetY() < Height * 0.7f)
                    {
                        _joyActive = true;
                        _joyOx = e.GetX(); _joyOy = e.GetY(); _jdx = _jdy = 0;
                    }
                    break;
                }
                if (Java.Lang.JavaSystem.CurrentTimeMillis() - _endedAt > 600)
                {
                    if (InBtn(e, 2)) ResetRun(_survival);
                    else if (InBtn(e, 3)) _phase = Phase.Menu;
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

    private void ResetRun() => ResetRun(false);

    private void ResetRun(bool survival)
    {
        _survival = survival;
        _nextSurvBoss = 18f;
        _px = _py = 0; _hp = GameBalance.Player_MaxHP;
        _level = 1; _levelUps = 0; _xp = 0;
        _mobs.Clear(); _shots.Clear(); _orbs.Clear(); _bladeTicks.Clear(); _touchCd.Clear();
        _spawnNumber = 0; _kills = _elites = _bossesKilled = 0; _eliteDropIndex = 0;
        Array.Clear(_bossSpawned);
        _bossAlive = null;
        _runTime = 0; _bladeAngle = 0;
        _spawnCd = 0.8f; _grace = 3f; _hb = 0;
        SimReset();
        InitRunLoadout();
        _phase = Phase.Run;
    }

    private float ScaleNow => Kinematics.ScaleAfterLevelUps(_levelUps);
    private int BladesN => _bladesOn ? _bladesN : 0;
    private float BladeR => _bladeR;
    private float MagnetR => _magnetR;
    private float OrthoNow => Kinematics.OrthoTarget(ScaleNow);

    private void Update(float dt)
    {
        _runTime += dt;
        float minutes = _runTime / 60f;

        float mag = MathF.Min(MathF.Sqrt(_jdx * _jdx + _jdy * _jdy), 1f);
        float speed = Kinematics.FinalSpeed(mag, ScaleNow, PassLvl(PassiveId.Speed), MetaSteps(4));
        float jm2 = MathF.Sqrt(_jdx * _jdx + _jdy * _jdy);
        if (jm2 > 0.05f)
        {
            _face = MathF.Atan2(_jdy, _jdx) * 180f / MathF.PI + 90f;
            _dirX = _jdx / jm2; _dirY = _jdy / jm2;
        }
        float l = MathF.Max(jm2, 1e-5f);
        _px = Math.Clamp(_px + _jdx / l * speed * dt, -ArenaClamp, ArenaClamp);
        _py = Math.Clamp(_py + _jdy / l * speed * dt, -ArenaClamp, ArenaClamp);

        WeaponsTick(dt);
        Blades(minutes);
        RegenTick(dt);
        JuiceTick(dt);
        if (_pendingCards > 0 && _phase == Phase.Run) OpenLevelUpCards();

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
        _hb += dt;
        if (_hb >= 10)
        {
            _hb = 0;
            global::Android.Util.Log.Info("Game",
                $"HB t={_runTime:0} hp={_hp:0} lvl={_level} kills={_kills} mobs={_mobs.Count(m => m.Hp > 0)} grace={_grace:0.0}");
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
        if (!_bladesOn) return;
        float dmg = _bladeStats.Damage * PowerMult;
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
                float kx = m.X - _px, ky = m.Y - _py, kl = MathF.Max(MathF.Sqrt(kx * kx + ky * ky), 1e-3f);
                m.X += kx / kl * 0.12f; m.Y += ky / kl * 0.12f;
            }
        }
        _bladeTicks.RemoveAll(t => t.Mob.Hp <= 0 || !_mobs.Contains(t.Mob));
    }

    private void Director(float dt, float minutes)
    {
        if (_survival)
        {
            if (!_bossSpawned[0] || _bossAlive is { Hp: > 0 } || minutes < _nextSurvBoss) return;
            BossStats st = WaveScript.SurvivalBossAt(_nextSurvBoss);
            var sb = new Mob { Kind = EnemyKind.Tank, Boss = true,
                X = SpawnPos().X, Y = SpawnPos().Y, Hp = st.Hp, Speed = st.Speed, Damage = st.ContactDamage, Size = 2.2f };
            _mobs.Add(sb);
            _bossAlive = sb;
            _nextSurvBoss += GameBalance.Surv_BossRepeatEveryMin;
            return;
        }

        for (int i = 0; i < WaveScript.BossScheduleMinutes.Length; i++)
        {
            if (_bossSpawned[i] || minutes < WaveScript.BossScheduleMinutes[i]) continue;
            _bossSpawned[i] = true;
            BossStats st = WaveScript.BossByIndex(i);
            var b = new Mob { Kind = EnemyKind.Tank, Boss = true, FinalBoss = i == 3,
                X = SpawnPos().X, Y = SpawnPos().Y, Hp = st.Hp, Speed = st.Speed, Damage = st.ContactDamage, Size = 2.2f };
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
        if (bossFight || _grace > 0) return;

        _spawnCd -= dt;
        if (_spawnCd > 0) return;
        _spawnCd = _survival
            ? WaveScript.SurvivalSpawnInterval(minutes)
            : WaveScript.SpawnInterval(_spawnNumber);

        int cap = _survival ? WaveScript.SurvivalAliveCap(minutes) : WaveScript.AliveCap(minutes);
        int alive = _mobs.Count(m => m.Hp > 0);
        if (alive >= cap) return;

        _spawnNumber++;
        int batch = _survival ? WaveScript.SurvivalBatch(minutes) : WaveScript.BatchSize(minutes);
        batch = Math.Min(batch, cap - alive);
        for (int i = 0; i < batch; i++)
            Spawn(WaveScript.RollKind(minutes, _rng.NextDouble()), minutes, false);
    }

    private void Spawn(EnemyKind kind, float minutes, bool elite)
    {
        EnemyBaseStats b = WaveScript.Base(kind);
        _mobs.Add(new Mob
        {
            Kind = kind,
            X = SpawnPos().X, Y = SpawnPos().Y,
            Hp = WaveScript.ScaledHp(minutes, kind) * (elite ? WaveScript.WaveConstants.Elite_HpMult : 1),
            MaxHp = WaveScript.ScaledHp(minutes, kind) * (elite ? WaveScript.WaveConstants.Elite_HpMult : 1),
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

    private (float X, float Y) SpawnPos()
    {
        float halfW = OrthoNow * Aspect + GameBalance.Spawn_RingExtra;
        float halfH = OrthoNow + GameBalance.Spawn_RingExtra;
        float r = MathF.Max(halfW, halfH);
        for (int i = 0; i < 8; i++)
        {
            double a = _rng.NextDouble() * Math.PI * 2;
            float x = Math.Clamp(_px + MathF.Cos((float)a) * r, -ArenaClamp, ArenaClamp);
            float y = Math.Clamp(_py + MathF.Sin((float)a) * r, -ArenaClamp, ArenaClamp);
            float dx = x - _px, dy = y - _py;
            if (dx * dx + dy * dy >= GameBalance.Spawn_RerollMinDist * GameBalance.Spawn_RerollMinDist || i == 7)
                return (x, y);
        }
        return (_px + r, _py);
    }

    private float Aspect => Width / (float)Math.Max(1, Height);

    private void Mobs(float dt)
    {
        for (int i = _mobs.Count - 1; i >= 0; i--)
        {
            Mob m = _mobs[i];
            if (m.Hp <= 0) { _mobs.RemoveAt(i); continue; }

            float dx = _px - m.X, dy = _py - m.Y, d = MathF.Sqrt(dx * dx + dy * dy);

            if (m.Boss) { BossAi(m, dt, dx, dy, d); }
            else if (d > 0.01f) { m.X += dx / d * m.Speed * dt; m.Y += dy / d * m.Speed * dt; }

            if (m.Boss) { TouchCheck(m, d, dt); continue; }

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

    private void TouchCheck(Mob m, float d, float dt)
    {
        float touch = 0.45f * ScaleNow + 0.4f * m.Size;
        if (d < touch && _runTime - _touchCd.GetValueOrDefault(m, -9f) >= GameBalance.Contact_HitCooldownEnemy)
        {
            _touchCd[m] = _runTime;
            Hurt(m.Damage);
        }
    }

    private void BossAi(Mob m, float dt, float dx, float dy, float d)
    {
        m.PatternCd -= dt;
        switch (m.BossState)
        {
            case 0:
                if (d > 0.01f)
                {
                    float sp = m.Speed * 0.6f;
                    m.X += dx / d * sp * dt;
                    m.Y += dy / d * sp * dt;
                }
                if (m.PatternCd <= 0)
                {
                    bool ring = m.Slot == 1 || (m.Slot == 3 && m.SecondDash);
                    m.Pattern = ring ? 1 : 0;
                    m.StateTimer = ring ? WaveScript.WaveConstants.Ring_TelegraphSec
                                        : WaveScript.WaveConstants.Dash_TelegraphSec;
                    m.TgtX = _px; m.TgtY = _py;
                    m.BossState = 1;
                    Play("boss", 0.6f);
                }
                break;

            case 1:
                m.StateTimer -= dt;
                if (m.StateTimer > 0) break;
                if (m.Pattern == 0)
                {
                    (float nx, float ny) = Norm(m.TgtX - m.X, m.TgtY - m.Y);
                    m.DashVx = nx * WaveScript.WaveConstants.Dash_Speed;
                    m.DashVy = ny * WaveScript.WaveConstants.Dash_Speed;
                    m.StateTimer = 1.2f;
                    m.BossState = 2;
                }
                else
                {
                    for (int k = 0; k < WaveScript.WaveConstants.Ring_Projectiles; k++)
                    {
                        float a2 = k * MathF.PI * 2f / WaveScript.WaveConstants.Ring_Projectiles;
                        _shots.Add(new Shot { X = m.X, Y = m.Y,
                            Vx = MathF.Cos(a2) * WaveScript.WaveConstants.Ring_ProjectileSpeed,
                            Vy = MathF.Sin(a2) * WaveScript.WaveConstants.Ring_ProjectileSpeed,
                            Life = 3f, Damage = m.Damage });
                    }
                    FinishBossPattern(m);
                }
                break;

            case 2:
                m.StateTimer -= dt;
                m.X += m.DashVx * dt;
                m.Y += m.DashVy * dt;
                m.X = Math.Clamp(m.X, -ArenaClamp, ArenaClamp);
                m.Y = Math.Clamp(m.Y, -ArenaClamp, ArenaClamp);
                if (m.StateTimer <= 0)
                {
                    if (m.Slot == 2 && !m.SecondDash)
                    {
                        m.SecondDash = true;
                        m.PatternCd = 0.4f;
                        m.BossState = 0;
                    }
                    else FinishBossPattern(m);
                }
                break;
        }
    }

    private void FinishBossPattern(Mob m)
    {
        m.BossState = 0;
        m.SecondDash = false;
        m.PatternCd = WaveScript.WaveConstants.Dash_Cooldown;
    }

    private void Hurt(float raw)
    {
        if (_phase != Phase.Run) return;
        if (_runTime - _lastHurt < 0.5f) return;
        _hp = Math.Max(0f, _hp - MathF.Max(1f, raw - ArmorFlat));
        _lastHurt = _runTime;
        if (_hp <= 0f)
        {
            _phase = Phase.Dead;
            _endedAt = Java.Lang.JavaSystem.CurrentTimeMillis();
            if (_survival && _runTime > _bestSurv) _bestSurv = _runTime;
            AwardRunEnd();
            global::Android.Util.Log.Info("Game", $"DEAD t={_runTime:0}s mode={(_survival ? "surv" : "camp")} kills={_kills}");
            Play("death", 0.9f);
        }
    }

    private void HurtMob(Mob m, float dmg)
    {
        m.Hp -= dmg;
        m.HitAt = _runTime;
        SpawnDmg(m.X, m.Y - 0.3f, MathF.Round(dmg), dmg >= 26f);
        if (_runTime - _lastHitSnd > 0.06f) { _lastHitSnd = _runTime; Play("hit", 0.35f); }
        if (m.Hp > 0) return;
        _kills++;
        if (m.Elite) _elites++;
        if (m.Boss)
        {
            _bossesKilled++;
            _orbs.Add(new Orb { X = m.X, Y = m.Y, Value = GameBalance.Boss_XP });
            OpenBigChest();
            if (ReferenceEquals(_bossAlive, m)) _bossAlive = null;
            m.Hp = 0;
            DeathBurst(m);
            if (m.FinalBoss) { _phase = Phase.Victory; _endedAt = Java.Lang.JavaSystem.CurrentTimeMillis(); AwardRunEnd(); }
            return;
        }
        if (m.Elite)
        {
            if (_eliteDropIndex % 2 == 0)
                _orbs.Add(new Orb { X = m.X, Y = m.Y, Value = GameBalance.Elite_BombXP });
            else
                OpenSmallChest();
            _eliteDropIndex++;
        }
        else
        {
            int[] xp = { 1, 1, 3, 2, 1, 1 };
            _orbs.Add(new Orb { X = m.X, Y = m.Y, Value = xp[(int)m.Kind] });
        }
        DeathBurst(m);
        Play("kill", 0.5f);
        m.Hp = 0;
    }

    private void Kill(Mob m, bool rewards) { if (!rewards) m.Hp = 0; }

    private void AddXp(double v)
    {
        _xp += v * XpMult;
        while (_xp >= XpCurve.Need(_level))
        {
            _xp -= XpCurve.Need(_level);
            _level++; _levelUps++;
            _pendingCards++;
        }
    }

    private void Sprite(Canvas c, string name, float wx, float wy, float sizeU, float angleDeg, int alpha = 255)
    {
        if (!_art.TryGetValue(name, out Bitmap? bmp) || bmp == null) return;
        float ppu = Height / (OrthoNow * 2f);
        float px = Width / 2f + (wx - _px) * ppu;
        float py = Height / 2f + (wy - _py) * ppu;
        float half = sizeU * ppu / 2f;
        if (px < -half * 2 || px > Width + half * 2 || py < -half * 2 || py > Height + half * 2) return;
        c.Save();
        c.Rotate(angleDeg, px, py);
        _bp.Alpha = alpha;
        c.DrawBitmap(bmp, null, new RectF(px - half, py - half, px + half, py + half), _bp);
        _bp.Alpha = 255;
        c.Restore();
    }

    private void DrawFloor(Canvas c)
    {
        float ppu = Height / (OrthoNow * 2f);
        Bitmap? tile = _art.GetValueOrDefault("tile");
        if (tile == null) return;
        float halfW = OrthoNow * Aspect + 2f, halfH = OrthoNow + 2f;
        for (int gy = -10; gy <= 10; gy++)
        {
            for (int gx = -10; gx <= 10; gx++)
            {
                float wx = gx * 2f, wy = gy * 2f;
                if (MathF.Abs(wx - _px) > halfW || MathF.Abs(wy - _py) > halfH) continue;
                float sx = Width / 2f + (wx - _px) * ppu;
                float sy = Height / 2f + (wy - _py) * ppu;
                c.DrawBitmap(tile, null, new RectF(sx, sy, sx + 2f * ppu, sy + 2f * ppu), _bp);
                int h = Math.Abs(gx * 73856093 ^ gy * 19349663) % 17;
                if (h == 0) Sprite(c, "decor_rock", wx + 1.1f, wy + 0.8f, 0.7f, 0, 170);
                else if (h == 5) Sprite(c, "decor_grass", wx + 0.9f, wy + 1.2f, 0.6f, 0, 150);
                else if (h == 11) Sprite(c, "decor_skull", wx + 1.3f, wy + 0.7f, 0.5f, 0, 150);
            }
        }
    }

    private void Render(Canvas c)
    {
        c.DrawRGB(35, 37, 56);
        float ppu = Height / (OrthoNow * 2f);
        float cx = Width / 2f, cy = Height / 2f;
        float X(float wx) => cx + (wx - _px) * ppu;
        float Y(float wy) => cy + (wy - _py) * ppu;

        DrawFloor(c);

        _p.SetStyle(Paint.Style.Stroke);
        _p.StrokeWidth = 2;
        _p.Color = Color.Rgb(61, 64, 96);
        c.DrawRect(X(-ArenaHalf), Y(-ArenaHalf), X(ArenaHalf), Y(ArenaHalf), _p);

        foreach (Orb o in _orbs)
            Sprite(c, o.Value > 5 ? "orb5" : "orb", o.X, o.Y, o.Value > 5 ? 0.45f : 0.3f, 0);

        foreach (Mob m in _mobs)
        {
            if (m.Hp <= 0) continue;
            float ang = MathF.Atan2(_py - m.Y, _px - m.X) * 180f / MathF.PI + 90f;
            float sizeU = (m.Boss ? 2.3f : KindSizeU[(int)m.Kind]) * MathF.Pow(m.Size, 0.5f);
            _p.SetStyle(Paint.Style.Fill);
            _p.Color = Color.Argb(80, 0, 0, 0);
            float shX = X(m.X), shY = Y(m.Y) + 0.15f * ppu;
            c.DrawOval(new RectF(shX - sizeU * ppu * 0.4f, shY - sizeU * ppu * 0.16f,
                                 shX + sizeU * ppu * 0.4f, shY + sizeU * ppu * 0.16f), _p);
            bool blink = m.Kind == EnemyKind.Kamikaze;
            float dxk = m.X - _px, dyk = m.Y - _py;
            int alpha = blink && dxk * dxk + dyk * dyk < 2.56f
                ? (MathF.Floor(Java.Lang.JavaSystem.CurrentTimeMillis() / 80f) % 2 == 0 ? 120 : 255) : 255;
            Sprite(c, m.Sprite, m.X, m.Y, sizeU, ang, alpha);
            if (m.Elite || m.Boss)
            {
                _p.SetStyle(Paint.Style.Stroke);
                _p.StrokeWidth = 3;
                _p.Color = Color.Rgb(239, 71, 111);
                c.DrawCircle(X(m.X), Y(m.Y), 0.55f * m.Size * ppu + 5, _p);
            }

            if (m.Boss && m.BossState == 1)
            {
                float pulse = 0.5f + 0.5f * MathF.Floor(_runTime * 8f % 2f);
                _p.SetStyle(Paint.Style.Stroke);
                _p.StrokeWidth = 3;
                _p.Color = Color.Argb((int)(140 + 80 * pulse), 239, 71, 111);
                if (m.Pattern == 0)
                {
                    c.DrawLine(X(m.X), Y(m.Y), X(m.TgtX), Y(m.TgtY), _p);
                    c.DrawCircle(X(m.TgtX), Y(m.TgtY), 12 * _density, _p);
                }
                else
                {
                    float rr = (1f - m.StateTimer / WaveScript.WaveConstants.Ring_TelegraphSec) * 3.5f * ppu;
                    c.DrawCircle(X(m.X), Y(m.Y), MathF.Max(8f, rr), _p);
                }
            }
        }

        if (_phase == Phase.Run)
        {
            for (int b = 0; _bladesOn && b < BladesN; b++)
            {
                float a = (_bladeAngle + 360f / BladesN * b) * MathF.PI / 180f;
                Sprite(c, "sword", _px + MathF.Cos(a) * BladeR, _py + MathF.Sin(a) * BladeR,
                    0.75f * ScaleNow, a + 90f);
            }
            _p.SetStyle(Paint.Style.Fill);
            _p.Color = Color.Argb(80, 0, 0, 0);
            c.DrawOval(new RectF(cx - 0.4f * ScaleNow * ppu, cy + 0.28f * ScaleNow * ppu,
                                 cx + 0.4f * ScaleNow * ppu, cy + 0.44f * ScaleNow * ppu), _p);
            Sprite(c, "player", _px, _py, 0.95f * ScaleNow, _face,
                _runTime - _lastHurt < 0.12f ? 140 : 255);
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
        c.DrawRect(0, 0, Width * Math.Clamp(_hp / _maxHp, 0f, 1f), 5 * _density, _p);

        RenderOverlays(c, cx, cy);

        if (_phase == Phase.Run)
        {
            var pb = Btn(6);
            _p.SetStyle(Paint.Style.Fill);
            _p.Color = Color.Argb(140, 43, 45, 66);
            c.DrawRoundRect(pb, 10 * _density, 10 * _density, _p);
            _p.SetStyle(Paint.Style.Fill);
            _p.Color = Color.White;
            float mx = (pb.Left + pb.Right) / 2f, my = (pb.Top + pb.Bottom) / 2f;
            c.DrawRect(mx - 7 * _density, my - 12 * _density, mx - 2 * _density, my + 12 * _density, _p);
            c.DrawRect(mx + 2 * _density, my - 12 * _density, mx + 7 * _density, my + 12 * _density, _p);

            if (_grace > 0)
            {
                _p.Color = Color.White;
                _p.TextSize = 90 * _density;
                string cd = MathF.Ceiling(_grace).ToString("0");
                c.DrawText(cd, cx - _p.MeasureText(cd) / 2, cy, _p);
                _p.TextSize = 14 * _density;
                _p.Color = Color.Rgb(184, 189, 212);
                string g = "тяни — движение";
                c.DrawText(g, cx - _p.MeasureText(g) / 2, cy + 44 * _density, _p);
            }
        }

        if (_phase == Phase.Menu)
        {
            _p.Color = Color.Argb(215, 20, 21, 38);
            c.DrawRect(0, 0, Width, Height, _p);
            _p.Color = Color.White;
            _p.TextSize = 44 * _density;
            string logo = "РУБИЛОВО";
            c.DrawText(logo, cx - _p.MeasureText(logo) / 2, Height * 0.17f, _p);
            _p.TextSize = 15 * _density;
            _p.Color = Color.Rgb(184, 189, 212);
            string tag = "растёй · крути · руби · выживи";
            c.DrawText(tag, cx - _p.MeasureText(tag) / 2, Height * 0.21f, _p);
            DrawBtn(c, 0, "КАМПАНИЯ · 15 МИН");
            DrawBtn(c, 1, "ВЫЖИВАНИЕ · ∞");
            DrawBtn(c, 7, $"ДЕРЕВО СТАТОВ · 🍖 {_save.Meat}");
            _p.SetStyle(Paint.Style.Fill);
            _p.TextSize = 14 * _density;
            _p.Color = Color.Rgb(184, 189, 212);
            string rec = _bestSurv > 0 ? $"рекорд выживания: {_bestSurv:0} сек" : "карточки апгрейдов — каждые 20-30 секунд";
            c.DrawText(rec, cx - _p.MeasureText(rec) / 2, Height * 0.70f, _p);
            return;
        }

        if (_phase == Phase.Paused)
        {
            _p.Color = Color.Argb(190, 20, 21, 38);
            c.DrawRect(0, 0, Width, Height, _p);
            _p.Color = Color.White;
            _p.TextSize = 34 * _density;
            string pt = "ПАУЗА";
            c.DrawText(pt, cx - _p.MeasureText(pt) / 2, Height * 0.30f, _p);
            DrawBtn(c, 4, "ПРОДОЛЖИТЬ");
            DrawBtn(c, 5, "В МЕНЮ");
            return;
        }

        if (_phase is Phase.Dead or Phase.Victory)
        {
            _p.Color = Color.Argb(200, 20, 21, 38);
            c.DrawRect(0, 0, Width, Height, _p);
            _p.Color = Color.White;
            _p.TextSize = 34 * _density;
            string title = (_phase == Phase.Victory ? "ПОБЕДА!" : "ПОРАЖЕНИЕ") + (_survival ? " · ∞" : "");
            c.DrawText(title, cx - _p.MeasureText(title) / 2, cy - 70 * _density, _p);
            _p.TextSize = 17 * _density;
            string sub = $"время {mm}:{ss:00}   уровень {_level}   убийства {_kills}";
            c.DrawText(sub, cx - _p.MeasureText(sub) / 2, cy - 34 * _density, _p);
            string sub2 = $"элиты {_elites}   боссы {_bossesKilled}   мясо +{meat}";
            c.DrawText(sub2, cx - _p.MeasureText(sub2) / 2, cy - 10 * _density, _p);
            DrawBtn(c, 2, "ЗАНОВО");
            DrawBtn(c, 3, "В МЕНЮ");
        }
    }

    private RectF Btn(int slot)
    {
        float w = Width * 0.52f, h = MathF.Max(52 * _density, Height * 0.05f);
        float cx = Width / 2f;
        if (slot == 6)
        {
            float sz = 46 * _density;
            return new RectF(Width - sz - 16 * _density, 30 * _density, Width - 16 * _density, 30 * _density + sz);
        }
        float cyF = slot switch { 0 => 0.34f, 1 => 0.47f, 2 => 0.60f, 3 => 0.725f, 4 => 0.42f, 5 => 0.565f, 7 => 0.60f, 8 => 0.885f, _ => 0.5f };
        float cy = cyF * Height;
        return new RectF(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2);
    }

    private bool InRect(MotionEvent e, RectF r) =>
        e.GetX() >= r.Left && e.GetX() <= r.Right && e.GetY() >= r.Top && e.GetY() <= r.Bottom;

    private bool InBtn(MotionEvent e, int slot)
    {
        RectF r = Btn(slot);
        return e.GetX() >= r.Left && e.GetX() <= r.Right && e.GetY() >= r.Top && e.GetY() <= r.Bottom;
    }

    private void DrawBtn(Canvas c, int slot, string label)
    {
        RectF r = Btn(slot);
        _p.SetStyle(Paint.Style.Fill);
        _p.Color = Color.Argb(220, 43, 45, 66);
        c.DrawRoundRect(r, 16 * _density, 16 * _density, _p);
        _p.SetStyle(Paint.Style.Stroke);
        _p.StrokeWidth = 2;
        _p.Color = Color.Rgb(255, 209, 102);
        c.DrawRoundRect(r, 16 * _density, 16 * _density, _p);
        _p.SetStyle(Paint.Style.Fill);
        _p.Color = Color.White;
        _p.TextSize = 18 * _density;
        c.DrawText(label, r.CenterX() - _p.MeasureText(label) / 2, r.CenterY() + 6 * _density, _p);
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
