using UnityEngine;

public class PlayerProjectiles : MonoBehaviour
{
    private static PlayerProjectiles instance;

    public static void SpawnStraight(Vector2 pos, Vector2 velocity, float lifeSec, float damage, int pierce)
    {
        EnsureInstance();
        var go = NewBullet(pos);
        var b = go.AddComponent<StraightBullet>();
        b.Velocity = velocity;
        b.LifeSec = lifeSec;
        b.Damage = damage;
        b.PierceLeft = pierce;
        b.Hit = new System.Collections.Generic.HashSet<Enemy>();
    }

    public static void SpawnArc(Vector2 pos, Vector2 velocity, float lifeSec, float damage)
    {
        EnsureInstance();
        var go = NewBullet(pos);
        var b = go.AddComponent<ArcBullet>();
        b.Velocity = velocity;
        b.LifeSec = lifeSec;
        b.Damage = damage;
        b.Hit = new System.Collections.Generic.HashSet<Enemy>();
    }

    private static GameObject NewBullet(Vector2 pos)
    {
        var go = new GameObject("playerShot");
        go.transform.SetParent(instance.transform);
        go.transform.position = pos;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.22f;
        return go;
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("PlayerProjectiles");
        instance = go.AddComponent<PlayerProjectiles>();
        DontDestroyOnLoad(go);
    }
}

public abstract class PlayerBulletBase : MonoBehaviour
{
    public Vector2 Velocity;
    public float LifeSec;
    public float Damage;
    public System.Collections.Generic.HashSet<Enemy> Hit;

    protected virtual void Update()
    {
        LifeSec -= Time.deltaTime;
        if (LifeSec <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(Velocity * Time.deltaTime);
    }

    protected bool TryHit(Collider2D other)
    {
        if (!other.TryGetComponent(out Enemy e)) return false;
        if (!e.IsActive || !Hit.Add(e)) return false;
        e.TakeDamage(Damage, transform.position);
        return true;
    }
}

public class StraightBullet : PlayerBulletBase
{
    public int PierceLeft;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!TryHit(other)) return;
        if (PierceLeft > 0) PierceLeft--;
        else Destroy(gameObject);
    }
}

public class ArcBullet : PlayerBulletBase
{
    private void FixedUpdate()
    {
        Velocity += Vector2.down * (18f * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }
}
