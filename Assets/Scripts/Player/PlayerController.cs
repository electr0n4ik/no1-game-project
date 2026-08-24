using Rubilovo.Logic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float acceleration = 24f;
    [SerializeField] private Health health;
    [SerializeField] private CameraFollow cameraFollow;

    private Rigidbody2D body;
    private PassiveEffects effects;
    private Vector2 input;
    private Vector2 lastFacing = Vector2.right;
    private float regenCarry;

    public Vector2 Facing => lastFacing;

    private static Vector2 touchOrigin;
    private static bool dragging;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        effects = GetComponent<PassiveEffects>();
        health.Damaged += OnDamaged;
        health.Died += OnDied;
        RefreshMaxHp();
    }

    private void OnDestroy()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    public void RefreshMaxHp()
    {
        float max = effects != null ? effects.MaxHp(GameBalance.Player_MaxHP) : GameBalance.Player_MaxHP;
        health.SetMax(max);
        if (effects != null) health.ArmorFlat = effects.ArmorFlat;
    }

    private void Update()
    {
        input = ReadInput();
        if (input.sqrMagnitude > 0.01f) lastFacing = input.normalized;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        float mag = Mathf.Min(input.magnitude, 1f);
        float scale = transform.localScale.x;
        int spLvl = effects != null ? effects.GetLevel(PassiveId.Speed) : 0;
        int steps = effects != null ? effects.Meta.SpeedSteps : 0;
        Vector2 targetVelocity = mag > 0f
            ? input.normalized * Kinematics.FinalSpeed(mag, scale, spLvl, steps)
            : Vector2.zero;
        body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

        ApplyArenaClamp();

        if (effects != null && effects.RegenPerSec > 0f && !health.IsDead)
        {
            regenCarry += effects.RegenPerSec * Time.fixedDeltaTime;
            if (regenCarry >= 1f)
            {
                int heal = (int)regenCarry;
                regenCarry -= heal;
                health.HealFlat(heal);
            }
        }
    }

    private void ApplyArenaClamp()
    {
        Vector2 p = body.position;
        float lim = GameBalance.Arena_Clamp;
        bool clamped = false;
        if (p.x > lim) { p.x = lim; clamped = true; }
        if (p.x < -lim) { p.x = -lim; clamped = true; }
        if (p.y > lim) { p.y = lim; clamped = true; }
        if (p.y < -lim) { p.y = -lim; clamped = true; }
        if (!clamped) return;
        body.position = p;
        Vector2 v = body.linearVelocity;
        if ((p.x >= lim && v.x > 0f) || (p.x <= -lim && v.x < 0f)) v.x = 0f;
        if ((p.y >= lim && v.y > 0f) || (p.y <= -lim && v.y < 0f)) v.y = 0f;
        body.linearVelocity = v;
    }

    public void Revive()
    {
        RefreshMaxHp();
        health.FullHeal();
    }

    private void OnDamaged(Health _)
    {
        if (cameraFollow != null) cameraFollow.Punch(0.15f, 0.12f);
    }

    private void OnDied(Health _) => GameManager.Instance.HandleDeath();

    private static Vector2 ReadInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        Vector2 keys = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (keys.sqrMagnitude > 0.01f) return keys;
#endif
        if (Input.touchCount == 0) return Vector2.zero;
        return TouchDirection(Input.GetTouch(0));
    }

    private static Vector2 TouchDirection(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                dragging = true;
                touchOrigin = touch.position;
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                dragging = false;
                break;
        }
        if (!dragging) return Vector2.zero;
        Vector2 delta = touch.position - touchOrigin;
        float dist = delta.magnitude;
        float dead = GameBalance.Input_DeadzonePx;
        float maxP = GameBalance.Input_MaxPixels;
        if (dist <= dead) return Vector2.zero;
        float magnitude01 = dist >= maxP ? 1f : (dist - dead) / (maxP - dead);
        return delta / dist * magnitude01;
    }
}
