using System.Collections;
using Rubilovo.Logic;
using UnityEngine;

public class WaveDirector : MonoBehaviour
{
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private XpOrb xpOrbPrefab;
    [SerializeField] private Transform spawnContainer;
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;

    private ObjectPool<Enemy> enemies;
    private ObjectPool<XpOrb> orbs;
    private readonly System.Random rng = new();

    private Coroutine loop;
    private int spawnNumber;
    private readonly bool[] bossSpawned = new bool[4];
    private Enemy bossAlive;
    private bool bossFightActive;
    private bool finalPending;
    private float eliteTimer;
    private int eliteDropIndex;

    private void Awake()
    {
        enemies = new ObjectPool<Enemy>(enemyPrefab, 60, spawnContainer);
        orbs = new ObjectPool<XpOrb>(xpOrbPrefab, 150, spawnContainer);
    }

    private void OnEnable()
    {
        GameManager.Instance.OnRunStarted += HandleRunStarted;
        GameManager.Instance.OnStateChanged += HandleState;
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnRunStarted -= HandleRunStarted;
        GameManager.Instance.OnStateChanged -= HandleState;
    }

    private void HandleState(GameState state)
    {
        switch (state)
        {
            case GameState.Dead:
                StopLoop();
                break;
            case GameState.Playing when !running:
                loop = StartCoroutine(SpawnLoop());
                break;
        }
    }

    private bool running;

    private float nextSurvivalBossMinute = 18f;

    private void TrySpawnSurvivalBoss(float minutes)
    {
        if (bossFightActive || minutes < nextSurvivalBossMinute) return;
        bossFightActive = true;
        Enemy boss = enemies.Get();
        boss.transform.position = RingPosition();
        boss.SetupBoss(player, WaveScript.SurvivalBossAt(nextSurvivalBossMinute), OnEnemyDeath);
        bossAlive = boss;
        nextSurvivalBossMinute += GameBalance.Surv_BossRepeatEveryMin;
        CameraFollow.Instance?.Punch(0.25f, 0.25f);
    }

    private void HandleRunStarted()
    {
        StopLoop();
        ResetPools();
        spawnNumber = 0;
        for (int i = 0; i < bossSpawned.Length; i++) bossSpawned[i] = false;
        bossAlive = null;
        bossFightActive = false;
        finalPending = false;
        nextSurvivalBossMinute = 18f;
        eliteDropIndex = 0;
        eliteTimer = UnityEngine.Random.Range(WaveScript.WaveConstants.Elite_TimerMinSec,
                                  WaveScript.WaveConstants.Elite_TimerMaxSec);
        RunTracker.Instance?.ResetRun();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
        float minutes = GameManager.Instance.RunTime / 60f;

        if (GameManager.Instance.CurrentMode == GameMode.Survival) TrySpawnSurvivalBoss(minutes);
        else TrySpawnBoss(minutes);

        if (bossFightActive && bossAlive == null)
            bossFightActive = false;

        if (!bossFightActive && minutes >= WaveScript.WaveConstants.Elite_FromMinute)
        {
            eliteTimer -= Time.deltaTime;
            if (eliteTimer <= 0f)
            {
                SpawnElite(minutes);
                eliteTimer = UnityEngine.Random.Range(WaveScript.WaveConstants.Elite_TimerMinSec,
                                          WaveScript.WaveConstants.Elite_TimerMaxSec);
            }
        }
    }

    private void TrySpawnBoss(float minutes)
    {
        if (bossFightActive) return;
        for (int i = 0; i < WaveScript.BossScheduleMinutes.Length; i++)
        {
            if (bossSpawned[i]) continue;
            if (minutes < WaveScript.BossScheduleMinutes[i]) continue;

            bossSpawned[i] = true;
            bossFightActive = true;
            finalPending = i == WaveScript.BossScheduleMinutes.Length - 1;

            Enemy boss = enemies.Get();
            boss.transform.position = RingPosition();
            boss.SetupBoss(player, WaveScript.BossByIndex(i), OnEnemyDeath);
            bossAlive = boss;
            CameraFollow.Instance?.Punch(0.25f, 0.25f);
            return;
        }
    }

    private void SpawnElite(float minutes)
    {
        Enemy elite = null;
        foreach (Enemy candidate in enemies.ActiveSnapshot())
        {
            if (candidate.IsBoss || candidate.IsElite) continue;
            elite = candidate;
            break;
        }
        if (elite != null)
            elite.Setup(player, elite.Kind, minutes, OnEnemyDeath, elite: true);
        else
        {
            elite = enemies.Get();
            elite.transform.position = RingPosition();
            elite.Setup(player, (EnemyKind)UnityEngine.Random.Range(0, 6), minutes, OnEnemyDeath, elite: true);
        }
    }

    private IEnumerator SpawnLoop()
    {
        running = true;
        bool survival = GameManager.Instance.CurrentMode == GameMode.Survival;
        while (true)
        {
            float wait = survival
                ? WaveScript.SurvivalSpawnInterval(GameManager.Instance.RunTime / 60f)
                : WaveScript.SpawnInterval(spawnNumber);
            yield return new WaitForSeconds(wait);
            if (GameManager.Instance.State != GameState.Playing) continue;
            if (bossFightActive) continue;
            SpawnWave();
        }
    }

    private void SpawnWave()
    {
        float minutes = GameManager.Instance.RunTime / 60f;
        bool survival = GameManager.Instance.CurrentMode == GameMode.Survival;
        int cap = survival ? WaveScript.SurvivalAliveCap(minutes) : WaveScript.AliveCap(minutes);
        if (enemies.ActiveCount >= cap) return;

        spawnNumber++;
        int batch = survival ? WaveScript.SurvivalBatch(minutes) : WaveScript.BatchSize(minutes);
        batch = Mathf.Min(batch, cap - enemies.ActiveCount);
        var kinds = WaveScript.RollWaveComposition(minutes, batch, rng);
        foreach (EnemyKind kind in kinds)
        {
            Enemy enemy = enemies.Get();
            enemy.transform.position = RingPosition();
            enemy.Setup(player, kind, minutes, OnEnemyDeath);
        }
    }

    private Vector3 RingPosition()
    {
        float halfHeight = mainCamera.orthographicSize + GameBalance.Spawn_RingExtra;
        float halfWidth = halfHeight * mainCamera.aspect;
        float r = Mathf.Max(halfWidth, halfHeight);
        Vector2 center = player.position;
        float lim = GameBalance.Arena_Clamp;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            Vector2 p = center + dir * r;
            p.x = Mathf.Clamp(p.x, -lim, lim);
            p.y = Mathf.Clamp(p.y, -lim, lim);
            if (Vector2.Distance(p, center) >= GameBalance.Spawn_RerollMinDist || attempt == 7) return p;
        }
        return center + Vector2.up * r;
    }

    private void OnEnemyDeath(Enemy enemy)
    {
        if (!enemy.RewardlessDeath)
        {
            RunTracker.Instance?.RegisterKill(enemy.Kind, enemy.IsElite, enemy.IsBoss);

            if (enemy.IsBoss)
            {
                DropOrb(enemy.transform.position, GameBalance.Boss_XP);
                LevelUpController.Instance?.OpenBigChest();
                bossAlive = null;
                if (finalPending && GameManager.Instance.CurrentMode == GameMode.Campaign)
                {
                    finalPending = false;
                    GameManager.Instance.Victory();
                }
            }
            else if (enemy.IsElite)
            {
                if (eliteDropIndex % 2 == 0) DropOrb(enemy.transform.position, GameBalance.Elite_BombXP);
                else LevelUpController.Instance?.OpenSmallChest();
                eliteDropIndex++;
            }
            else if (enemy.XpValue > 0f)
            {
                DropOrb(enemy.transform.position, enemy.XpValue);
            }
        }

        enemies.Release(enemy);
    }

    private void DropOrb(Vector3 position, float value)
    {
        XpOrb orb = orbs.Get();
        orb.transform.position = position;
        orb.Init(player, value, o => orbs.Release(o));
    }

    private void StopLoop()
    {
        running = false;
        if (loop != null) StopCoroutine(loop);
        loop = null;
    }

    private void ResetPools()
    {
        enemies.ReleaseAll();
        orbs.ReleaseAll();
    }
}
