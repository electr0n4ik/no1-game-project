using Rubilovo.Logic;
using System.Collections.Generic;
using Rubilovo.Logic;
using UnityEngine;

public interface IWeaponRuntime
{
    void Init(WeaponContext ctx);
    void SetLevel(int lvl);
    void Evolve();
    void Tick(float dt);
}

public class WeaponContext
{
    public readonly WeaponLoadout Loadout;
    public readonly Transform Player;
    public readonly PassiveEffects Effects;
    public readonly System.Random Rng = new();

    public WeaponContext(WeaponLoadout loadout, Transform player, PassiveEffects effects)
    {
        Loadout = loadout;
        Player = player;
        Effects = effects;
    }

    public Vector2 Position => Player.position;
    public Vector2 Facing => Loadout.Facing;
    public float Area => Effects.AreaMult;
}

public static class WeaponQueries
{
    public static List<Enemy> InCircle(Vector2 center, float radius)
    {
        var result = new List<Enemy>();
        foreach (Collider2D col in Physics2D.OverlapCircleAll(center, radius))
            if (col.TryGetComponent(out Enemy e) && e.IsActive)
                result.Add(e);
        return result;
    }

    public static Enemy Nearest(Vector2 center, float radius)
    {
        Enemy best = null;
        float bestDist = radius;
        foreach (Enemy e in InCircle(center, radius))
        {
            float d = Vector2.Distance(e.transform.position, center);
            if (d < bestDist) { bestDist = d; best = e; }
        }
        return best;
    }
}

public class WeaponLoadout : MonoBehaviour
{
    [SerializeField] private PassiveEffects effects;
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerGrowth growth;

    private readonly Dictionary<WeaponId, IWeaponRuntime> active = new();
    private WeaponContext ctx;

    public LoadoutState State { get; } = new();

    private void Awake()
    {
        if (effects == null) effects = GetComponent<PassiveEffects>();
        if (player == null) player = GetComponent<PlayerController>();
        ctx = new WeaponContext(this, transform, effects);
    }

    public Vector2 Facing => player != null ? player.Facing : Vector2.right;

    public bool IsEquipped(WeaponId id) => active.ContainsKey(id);

    public void Equip(WeaponId id, int level = 1)
    {
        if (active.ContainsKey(id)) return;
        IWeaponRuntime w = id switch
        {
            WeaponId.Blades => new BladesRuntime(),
            WeaponId.Daggers => new DaggersRuntime(),
            WeaponId.Axe => new AxeRuntime(),
            WeaponId.Lightning => new LightningRuntime(),
            WeaponId.Aura => new AuraRuntime(),
            _ => new WhipRuntime()
        };
        w.Init(ctx);
        w.SetLevel(level);
        active[id] = w;
        State.WeaponLevels[(int)id] = level;
        State.WeaponCount++;
    }

    public bool LevelUp(WeaponId id)
    {
        if (!active.TryGetValue(id, out IWeaponRuntime w)) return false;
        int current = State.WeaponLevels[(int)id];
        if (current >= WeaponsCatalog.MaxLevel) return false;
        int next = current + 1;
        State.WeaponLevels[(int)id] = next;
        w.SetLevel(next);
        return true;
    }

    public void Evolve(WeaponId id)
    {
        if (active.TryGetValue(id, out IWeaponRuntime w)) w.Evolve();
    }

    public void DealDamage(Enemy target, float raw, Vector2 from)
    {
        float overflow = growth != null ? Kinematics.PowerFromLevels(growth.LevelUps) : 1f;
        target.TakeDamage(raw * (effects != null ? effects.PowerMult : 1f) * overflow, from);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
        float dt = Time.deltaTime;
        foreach (KeyValuePair<WeaponId, IWeaponRuntime> kv in active) kv.Value.Tick(dt);
    }
}

public class BladesRuntime : IWeaponRuntime
{
    private WeaponContext ctx;
    private GameObject hitRoot;
    private CircleCollider2D area;
    private BladeHitbox hitbox;
    private readonly List<Transform> blades = new();
    private WeaponLevelStats s;
    private int level = 1;
    private bool evolved;
    private float angle;
    private float phaseTimer;

    public void Init(WeaponContext context)
    {
        ctx = context;
        hitRoot = new GameObject("bladesHitRoot");
        hitRoot.transform.SetParent(ctx.Player, false);
        area = hitRoot.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        hitbox = hitRoot.AddComponent<BladeHitbox>();
        hitbox.Owner = this;
        for (int i = 0; i < 4; i++)
        {
            var b = new GameObject($"blade{i}");
            b.transform.SetParent(hitRoot.transform);
            blades.Add(b.transform);
        }
        ApplyLevel(1);
    }

    public void SetLevel(int lvl) => ApplyLevel(lvl);

    private void ApplyLevel(int lvl)
    {
        level = lvl;
        s = WeaponsCatalog.Stats(WeaponId.Blades, lvl);
        area.radius = s.Radius * ctx.Area;
    }

    public void Evolve() => evolved = true;

    public void Tick(float dt)
    {
        angle = Mathf.Repeat(angle + s.SpeedDeg * dt, 360f);
        int count = Mathf.Min(s.Count, blades.Count);
        for (int i = 0; i < count; i++)
        {
            float a = (angle + 360f / count * i) * Mathf.Deg2Rad;
            blades[i].localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * area.radius;
        }
        for (int i = count; i < blades.Count; i++) blades[i].localPosition = Vector3.one * 999f;

        if (!evolved && level >= 4 && s.UptimeSec > 0f)
        {
            phaseTimer += dt;
            float cycle = s.UptimeSec + s.Extra;
            bool activePhase = phaseTimer % cycle < s.UptimeSec;
            if (area.enabled != activePhase) area.enabled = activePhase;
        }
        else if (!area.enabled) area.enabled = true;
    }

    public void HandleTouch(Collider2D other, Dictionary<Health, float> nextHits)
    {
        if (!other.TryGetComponent(out Health health)) return;
        if (Time.time < (nextHits.TryGetValue(health, out float t) ? t : 0f)) return;
        if (!other.TryGetComponent(out Enemy enemy) || !enemy.IsActive) return;
        nextHits[health] = Time.time + WeaponsCatalog.Blades_RehitSec;
        ctx.Loadout.DealDamage(enemy, s.Damage, ctx.Position);
    }
}

public class BladeHitbox : MonoBehaviour
{
    public BladesRuntime Owner;
    private readonly Dictionary<Health, float> nextHits = new();

    private void OnTriggerStay2D(Collider2D other) => Owner?.HandleTouch(other, nextHits);
}

public class DaggersRuntime : IWeaponRuntime
{
    private WeaponContext ctx;
    private WeaponLevelStats s;
    private float cd;

    public void Init(WeaponContext context) { ctx = context; s = WeaponsCatalog.Stats(WeaponId.Daggers, 1); }
    public void SetLevel(int lvl) => s = WeaponsCatalog.Stats(WeaponId.Daggers, lvl);
    public void Evolve() { }

    public void Tick(float dt)
    {
        cd -= dt * (ctx.Effects != null ? 1f / ctx.Effects.CooldownMult : 1f);
        if (cd > 0f) return;
        Enemy target = WeaponQueries.Nearest(ctx.Position, WeaponsCatalog.DaggerTargetRadius);
        if (target == null) return;
        cd = s.CooldownSec;
        Vector2 baseDir = (target.transform.position - (Vector3)ctx.Position).normalized;
        for (int i = 0; i < s.Count; i++)
        {
            float spread = (i - (s.Count - 1) * 0.5f) * 8f * Mathf.Deg2Rad;
            Vector2 dir = new(
                baseDir.x * Mathf.Cos(spread) - baseDir.y * Mathf.Sin(spread),
                baseDir.x * Mathf.Sin(spread) + baseDir.y * Mathf.Cos(spread));
            PlayerProjectiles.SpawnStraight(ctx.Position, dir * WeaponsCatalog.ProjectileSpeedDagger,
                WeaponsCatalog.DaggerLifeSec, ctx.Effects.PowerMult * s.Damage,
                Mathf.RoundToInt(s.Extra));
        }
    }
}

public class AxeRuntime : IWeaponRuntime
{
    private WeaponContext ctx;
    private WeaponLevelStats s;
    private float cd;

    public void Init(WeaponContext context) { ctx = context; s = WeaponsCatalog.Stats(WeaponId.Axe, 1); }
    public void SetLevel(int lvl) => s = WeaponsCatalog.Stats(WeaponId.Axe, lvl);
    public void Evolve() { }

    public void Tick(float dt)
    {
        cd -= dt / (ctx.Effects != null ? ctx.Effects.CooldownMult : 1f);
        if (cd > 0f) return;
        cd = s.CooldownSec;
        for (int i = 0; i < s.Count; i++)
        {
            float fan = UnityEngine.Random.Range(-WeaponsCatalog.AxeFanDegrees, WeaponsCatalog.AxeFanDegrees) * Mathf.Deg2Rad;
            Vector2 v = new(Mathf.Sin(fan) * 3.5f, 7f);
            PlayerProjectiles.SpawnArc(ctx.Position, v, 1.6f, ctx.Effects.PowerMult * s.Damage);
        }
    }
}

public class LightningRuntime : IWeaponRuntime
{
    private WeaponContext ctx;
    private WeaponLevelStats s;
    private float cd;

    public void Init(WeaponContext context) { ctx = context; s = WeaponsCatalog.Stats(WeaponId.Lightning, 1); }
    public void SetLevel(int lvl) => s = WeaponsCatalog.Stats(WeaponId.Lightning, lvl);
    public void Evolve() { }

    public void Tick(float dt)
    {
        cd -= dt / (ctx.Effects != null ? ctx.Effects.CooldownMult : 1f);
        if (cd > 0f) return;
        List<Enemy> pool = WeaponQueries.InCircle(ctx.Position, 12f);
        if (pool.Count == 0) return;
        cd = s.CooldownSec;
        int strikes = s.Count;
        while (strikes-- > 0 && pool.Count > 0)
        {
            Enemy target = pool[ctx.Rng.Next(pool.Count)];
            Strike(target);
            pool.Remove(target);
        }
    }

    private void Strike(Enemy main)
    {
        float r = s.Radius * ctx.Area;
        foreach (Enemy e in WeaponQueries.InCircle(main.transform.position, r))
            ctx.Loadout.DealDamage(e, s.Damage, main.transform.position);
    }
}

public class AuraRuntime : IWeaponRuntime
{
    private WeaponContext ctx;
    private WeaponLevelStats s;
    private int level = 1;
    private bool evolved;
    private float tick;
    private readonly Dictionary<Health, float> rehit = new();

    public void Init(WeaponContext context) { ctx = context; s = WeaponsCatalog.Stats(WeaponId.Aura, 1); }

    public void SetLevel(int lvl)
    {
        level = lvl;
        s = WeaponsCatalog.Stats(WeaponId.Aura, lvl);
    }

    public void Evolve() => evolved = true;

    public void Tick(float dt)
    {
        tick -= dt;
        if (tick > 0f) return;
        tick = WeaponsCatalog.Aura_TickSec;
        float radius = (evolved ? 3.0f : s.Radius) * ctx.Area;
        foreach (Enemy e in WeaponQueries.InCircle(ctx.Position, radius))
        {
            float key = rehit.TryGetValue(e.HealthRef, out float t) ? t : 0f;
            if (Time.time < key) continue;
            rehit[e.HealthRef] = Time.time + (evolved ? 0.75f : WeaponsCatalog.Aura_RehitSec);
            ctx.Loadout.DealDamage(e, evolved ? 14f : s.Damage, ctx.Position);
            float slowPct = evolved ? 0.25f : s.Extra;
            if (slowPct > 0f) e.ApplySlow(slowPct, 0.6f);
        }
    }
}

public class WhipRuntime : IWeaponRuntime
{
    private WeaponContext ctx;
    private WeaponLevelStats s;
    private int level = 1;
    private float cd;
    private float backswingAt = -1f;
    private Vector2 backswingDir;
    private float backswingPct;

    public void Init(WeaponContext context) { ctx = context; s = WeaponsCatalog.Stats(WeaponId.Whip, 1); }

    public void SetLevel(int lvl)
    {
        level = lvl;
        s = WeaponsCatalog.Stats(WeaponId.Whip, lvl);
    }

    public void Evolve() { }

    public void Tick(float dt)
    {
        cd -= dt / (ctx.Effects != null ? ctx.Effects.CooldownMult : 1f);
        if (backswingAt > 0f && Time.time >= backswingAt)
        {
            Slash(backswingDir, backswingPct);
            backswingAt = -1f;
        }
        if (cd > 0f) return;
        cd = s.CooldownSec;
        Vector2 facing = ctx.Facing == Vector2.zero ? Vector2.right : ctx.Facing;
        Slash(facing, 1f);
        if (WeaponsCatalog.HasBackswing(WeaponId.Whip, level))
        {
            backswingAt = Time.time + WeaponsCatalog.Whip_HitVisualSec;
            backswingDir = -facing;
            backswingPct = WeaponsCatalog.BackswingDamagePct(WeaponId.Whip, level);
        }
    }

    private void Slash(Vector2 dir, float pct)
    {
        Vector2 size = new(s.Radius * ctx.Area, s.Extra * ctx.Area);
        float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Vector2 center = ctx.Position + dir * (size.x * 0.5f);
        foreach (Collider2D col in Physics2D.OverlapBoxAll(center, size, angleDeg))
            if (col.TryGetComponent(out Enemy e) && e.IsActive)
                ctx.Loadout.DealDamage(e, s.Damage * pct, ctx.Position);
    }
}
