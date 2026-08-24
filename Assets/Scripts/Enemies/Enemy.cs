using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [SerializeField] private float contactDamage = 10f;
    [SerializeField] private float hitCooldown = 0.6f;

    private Rigidbody2D body;
    private Transform target;
    private float speed = 2.2f;
    private float hp = 18f;
    private float nextHitAt;
    private Action<Enemy> onDeath;

    public float XpReward { get; private set; } = 1f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Setup(Transform player, float enemyHp, float enemySpeed, Action<Enemy> deathCallback)
    {
        target = player;
        hp = enemyHp;
        speed = enemySpeed;
        XpReward = 1f + Mathf.Sqrt(enemyHp) * 0.12f;
        onDeath = deathCallback;
        body.linearVelocity = Vector2.zero;
        nextHitAt = 0f;
    }

    private void FixedUpdate()
    {
        if (target == null || GameManager.Instance.State != GameState.Playing)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }
        Vector2 direction = (target.position - transform.position).normalized;
        body.MovePosition(body.position + direction * (speed * Time.fixedDeltaTime));
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < nextHitAt) return;
        if (!other.TryGetComponent(out Health health)) return;
        nextHitAt = Time.time + hitCooldown;
        health.TakeDamage(contactDamage);
    }

    public void TakeDamage(float amount, Vector2 knockFrom)
    {
        hp -= amount;
        Vector2 knockDir = (body.position - knockFrom).normalized;
        body.MovePosition(body.position + knockDir * 0.1f);
        if (hp > 0f) return;
        onDeath?.Invoke(this);
    }
}
