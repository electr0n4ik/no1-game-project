using UnityEngine;

public class EnemyProjectiles : MonoBehaviour
{
    private static EnemyProjectiles instance;

    public static void Spawn(Vector2 position, Vector2 direction, float speed, float lifeSec, float damage)
    {
        EnsureInstance();
        instance.Fire(position, direction.normalized * speed, lifeSec, damage);
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("EnemyProjectiles");
        instance = go.AddComponent<EnemyProjectiles>();
        DontDestroyOnLoad(go);
    }

    private void Fire(Vector2 position, Vector2 velocity, float lifeSec, float damage)
    {
        var go = new GameObject("shot");
        go.transform.SetParent(transform);
        go.transform.position = position;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.15f;
        var bullet = go.AddComponent<ShotBullet>();
        bullet.Velocity = velocity;
        bullet.LifeSec = lifeSec;
        bullet.Damage = damage;
        go.SetActive(true);
    }
}

public class ShotBullet : MonoBehaviour
{
    public Vector2 Velocity;
    public float LifeSec;
    public float Damage;

    private void Update()
    {
        LifeSec -= Time.deltaTime;
        if (LifeSec <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(Velocity * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (other.TryGetComponent(out Health health))
        {
            health.TakeDamage(Damage);
            Destroy(gameObject);
        }
    }
}
