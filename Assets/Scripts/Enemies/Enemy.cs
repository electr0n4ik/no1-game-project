using System;
using Rubilovo.Logic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bodyRenderer;

    private Rigidbody2D body;
    private Transform target;

    public EnemyKind Kind { get; private set; } = EnemyKind.Walker;
    public bool IsElite { get; private set; }
    public bool IsBoss { get; private set; }
    public bool RewardlessDeath { get; private set; }
    public float XpValue { get; private set; } = 1f;

    private float hp;
    private float speed;
    private float contactDamage;
    private float nextContactAt;
    private float slowPct;
    private float slowUntil;
    private Action<Enemy> onDeath;

    public bool IsActive => hp > 0f && gameObject.activeInHierarchy;
    public Health HealthRef { get; private set; }

    private enum Mode { Chase, Shooter, KamikazeApproach, KamikazeTelegraph }
    private Mode mode;
    private float shooterCooldown;
    private float kamikazeTimer;

    private static readonly int[] XpByKind = { 1, 1, 3, 2, 1, 1 };

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(Transform player, EnemyKind kind, float elapsedMinutes, Action<Enemy> deathCallback,
                      bool elite = false)
    {
        target = player;
        Kind = kind;
        IsElite = elite;
        IsBoss = false;
        onDeath = deathCallback;

        EnemyBaseStats b = WaveScript.Base(kind);
        float mult = elite ? WaveScript.WaveConstants.Elite_HpMult : 1f;
        hp = WaveScript.ScaledHp(elapsedMinutes, kind) * mult;
        speed = b.Speed * (elite ? WaveScript.WaveConstants.Elite_SpeedMult : 1f);
        contactDamage = WaveScript.ScaledDamage(elapsedMinutes, kind) *
                        (elite ? WaveScript.WaveConstants.Elite_DamageMult : 1f);

        XpValue = elite ? 0f : XpByKind[(int)kind];
        float sizeMult = elite ? WaveScript.WaveConstants.Elite_SizeMult : 1f;
        transform.localScale = Vector3.one * sizeMult;
        if (bodyRenderer != null)
            bodyRenderer.color = elite ? new Color(1f, 0.35f, 0.35f) : Color.white;

        mode = kind switch
        {
            EnemyKind.Shooter => Mode.Shooter,
            EnemyKind.Kamikaze => Mode.KamikazeApproach,
            _ => Mode.Chase
        };
        shooterCooldown = 0f;
        kamikazeTimer = 0f;
        nextContactAt = 0f;
        slowPct = 0f;
        slowUntil = 0f;
        HealthRef = GetComponent<Health>();
        body.linearVelocity = Vector2.zero;
    }

    public void SetupBoss(Transform player, BossStats stats, Action<Enemy> deathCallback)
    {
        Setup(player, EnemyKind.Tank, stats.Minute, deathCallback, elite: false);
        IsBoss = true;
        hp = stats.Hp;
        speed = stats.Speed;
        contactDamage = stats.ContactDamage;
        XpValue = 0f;
        transform.localScale = Vector3.one * 2.2f;
        if (bodyRenderer != null) bodyRenderer.color = new Color(0.85f, 0.1f, 0.6f);
    }

    private float SpeedEff => Time.time < slowUntil ? speed * (1f - slowPct) : speed;

    public void ApplySlow(float pct, float durationSec)
    {
        if (pct <= 0f) return;
        slowPct = Mathf.Max(slowPct, pct);
        slowUntil = Time.time + durationSec;
    }

    private void FixedUpdate()
    {
        if (target == null || GameManager.Instance.State != GameState.Playing)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = target.position - transform.position;
        float dist = toPlayer.magnitude;
        Vector2 dir = dist > 0.001f ? toPlayer / dist : Vector2.zero;

        switch (mode)
        {
            case Mode.Chase:
                body.MovePosition(body.position + dir * (SpeedEff * Time.fixedDeltaTime));
                break;

            case Mode.Shooter:
            {
                float preferred = WaveScript.WaveConstants.Shooter_PreferredDist;
                if (dist > preferred + 0.5f)
                    body.MovePosition(body.position + dir * (SpeedEff * Time.fixedDeltaTime));
                else if (dist < preferred - 0.5f)
                    body.MovePosition(body.position - dir * (SpeedEff * Time.fixedDeltaTime));

                shooterCooldown -= Time.fixedDeltaTime;
                if (!(dist <= preferred + 2f) || !(shooterCooldown <= 0f)) break;
                shooterCooldown = WaveScript.WaveConstants.Shooter_FireCooldown;
                EnemyProjectiles.Spawn(transform.position, dir,
                    WaveScript.WaveConstants.Shooter_ProjectileSpeed,
                    WaveScript.WaveConstants.Shooter_ProjectileLife, contactDamage);
                break;
            }

            case Mode.KamikazeApproach:
            {
                body.MovePosition(body.position + dir * (SpeedEff * Time.fixedDeltaTime));
                if (dist < 2.0f)
                {
                    mode = Mode.KamikazeTelegraph;
                    kamikazeTimer = WaveScript.WaveConstants.Kamikaze_TelegraphSec;
                }
                break;
            }

            case Mode.KamikazeTelegraph:
            {
                kamikazeTimer -= Time.fixedDeltaTime;
                if (bodyRenderer != null)
                    bodyRenderer.enabled = Mathf.FloorToInt(Time.unscaledTime * 12f) % 2 == 0;
                if (kamikazeTimer > 0f) break;
                Explode(dist);
                break;
            }
        }
    }

    private void Explode(float distToPlayer)
    {
        float r = WaveScript.WaveConstants.Kamikaze_ExplosionRadius;
        if (distToPlayer <= r && target != null &&
            target.TryGetComponent(out Health playerHealth))
        {
            float dmg = WaveScript.ScaledDamage(GameManager.Instance.RunTime / 60f, Kind);
            playerHealth.TakeDamage(dmg);
        }
        Die(dropRewards: false);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < nextContactAt) return;
        if (!other.TryGetComponent(out Health health)) return;
        nextContactAt = Time.time + GameBalance.Contact_HitCooldownEnemy;
        health.TakeDamage(contactDamage);
    }

    public void TakeDamage(float amount, Vector2 knockFrom)
    {
        if (hp <= 0f) return;
        hp -= amount;
        RunTracker.Instance?.AddDamage(amount);
        if (!IsBoss)
        {
            Vector2 knockDir = (body.position - knockFrom).normalized;
            body.MovePosition(body.position + knockDir * 0.08f);
        }
        if (hp <= 0f) Die(dropRewards: true);
    }

    public void Die(bool dropRewards)
    {
        RewardlessDeath = !dropRewards;
        var callback = onDeath;
        onDeath = null;
        callback?.Invoke(this);
    }
}
