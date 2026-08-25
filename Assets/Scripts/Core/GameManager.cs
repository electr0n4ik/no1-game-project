using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameMode { Campaign, Survival }

[DefaultExecutionOrder(-90)]
public class GameManager : MonoBehaviour
{
    public GameMode CurrentMode { get; private set; } = GameMode.Campaign;
    public static GameManager Instance { get; private set; }

    [SerializeField] private float runDuration = 300f;

    public GameState State { get; private set; } = GameState.Menu;
    public float RunTime { get; private set; }
    public int Deaths { get; private set; }
    public bool LastRunWasVictory { get; private set; }

    public event Action<GameState> OnStateChanged;
    public event Action OnRunStarted;
    public event Action<float> OnRunTimer;

    private bool reviveUsedThisRun;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (State != GameState.Playing) return;
        RunTime += Time.deltaTime;
        OnRunTimer?.Invoke(RunTime);
        if (CurrentMode == GameMode.Campaign && RunTime >= runDuration) HandleDeath();
    }

    public void StartRun() => StartRun(GameMode.Campaign);

    public void StartRun(GameMode mode)
    {
        CurrentMode = mode;
        RunTime = 0f;
        LastRunWasVictory = false;
        reviveUsedThisRun = false;
        SaveSystem.EnsureDay();
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        OnRunStarted?.Invoke();
    }

    public float RunDuration => runDuration;

    public void HandleDeath()
    {
        if (State != GameState.Playing) return;
        Deaths++;
        LastRunWasVictory = false;
        CommitRunEnd();
        Time.timeScale = 0f;
        SetState(GameState.Dead);
    }

    public void Victory()
    {
        if (State != GameState.Playing || CurrentMode != GameMode.Campaign) return;
        LastRunWasVictory = true;
        CommitRunEnd();
        Time.timeScale = 0f;
        SetState(GameState.Dead);
    }

    public void TryRevive()
    {
        if (reviveUsedThisRun) return;
        AdsManager.Instance.TryShowRewarded(AdPlacements.Revive, rewarded =>
        {
            if (!rewarded) return;
            reviveUsedThisRun = true;
            var player = FindFirstByType<PlayerController>();
            if (player != null) player.Revive();
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        });
    }

    public void Restart()
    {
        AdsManager.Instance.MaybeShowInterstitial("restart");
        SceneLoader.ReloadGame();
    }

    private void CommitRunEnd()
    {
        if (CurrentMode == GameMode.Survival)
            SaveSystem.RecordSurvivalBest(RunTime);
        SaveSystem.RegisterRunEnd(RunTime);
    }

    private void SetState(GameState newState)
    {
        if (State == newState) return;
        State = newState;
        OnStateChanged?.Invoke(newState);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        Time.timeScale = 1f;
        State = GameState.Menu;
        OnStateChanged?.Invoke(GameState.Menu);
    }
}

public enum GameState { Menu, Playing, Dead }
