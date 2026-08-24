using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private XpOrb xpOrbPrefab;
    [SerializeField] private Transform spawnContainer;
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;

    [Header("Difficulty")]
    [SerializeField] private float spawnIntervalStart = 0.8f;
    [SerializeField] private float minInterval = 0.15f;
    [SerializeField] private float intervalRamp = 0.97f;
    [SerializeField] private int maxAlive = 80;
    [SerializeField] private float baseHp = 18f;
    [SerializeField] private float baseSpeed = 2.2f;

    private ObjectPool<Enemy> enemies;
    private ObjectPool<XpOrb> orbs;
    private Coroutine loop;
    private float interval;
    private bool running;

    private void Awake()
    {
        enemies = new ObjectPool<Enemy>(enemyPrefab, 60, spawnContainer);
        orbs = new ObjectPool<XpOrb>(xpOrbPrefab, 150, spawnContainer);
    }

    private void OnEnable()
    {
        GameManager.Instance.OnStateChanged += HandleState;
        GameManager.Instance.OnRunStarted += ResetPools;
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnStateChanged -= HandleState;
        GameManager.Instance.OnRunStarted -= ResetPools;
    }

    private void HandleState(GameState state)
    {
        switch (state)
        {
            case GameState.Dead:
                StopLoop();
                break;
            case GameState.Playing when !running:
                interval = spawnIntervalStart;
                loop = StartCoroutine(SpawnLoop());
                break;
        }
    }

    private void StopLoop()
    {
        running = false;
        if (loop != null) StopCoroutine(loop);
        loop = null;
    }

    private IEnumerator SpawnLoop()
    {
        running = true;
        while (true)
        {
            yield return new WaitForSeconds(interval);
            SpawnWave();
            interval = Mathf.Max(minInterval, interval * intervalRamp);
        }
    }

    private void SpawnWave()
    {
        if (enemies.ActiveCount >= maxAlive) return;
        float t = GameManager.Instance.RunTime;
        float difficulty = 1f + t / 45f;
        Enemy enemy = enemies.Get();
        enemy.transform.position = RingPosition();
        enemy.Setup(player, baseHp * difficulty, baseSpeed * (0.85f + difficulty * 0.08f), OnEnemyDeath);
    }

    private Vector3 RingPosition()
    {
        float halfHeight = mainCamera.orthographicSize + 1.5f;
        float halfWidth = halfHeight * mainCamera.aspect;
        Vector2 center = player.position;
        Vector2 dir = Random.insideUnitCircle.normalized;
        float r = Mathf.Max(halfWidth, halfHeight);
        return center + dir * r;
    }

    private void OnEnemyDeath(Enemy enemy)
    {
        XpOrb orb = orbs.Get();
        orb.transform.position = enemy.transform.position;
        orb.Init(player, enemy.XpReward, o => orbs.Release(o));
        enemies.Release(enemy);
    }

    private void ResetPools()
    {
        StopLoop();
        enemies.ReleaseAll();
        orbs.ReleaseAll();
    }
}
