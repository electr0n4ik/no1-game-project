using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-90)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float runDuration = 300f;

    public GameState State { get; private set; } = GameState.Menu;
    public float RunTime { get; private set; }
    public int Deaths { get; private set; }

    public event Action<GameState> OnStateChanged;
    public event Action OnRunStarted;
    public event Action<float> OnRunTimer;

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
        if (RunTime >= runDuration) HandleDeath();
    }

    public void StartRun()
    {
        RunTime = 0f;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        OnRunStarted?.Invoke();
    }

    public void HandleDeath()
    {
        if (State != GameState.Playing) return;
        Deaths++;
        Time.timeScale = 0f;
        SetState(GameState.Dead);
    }

    public void TryRevive()
    {
        AdsManager.Instance.TryShowRewarded("revive", rewarded =>
        {
            if (!rewarded) return;
            var player = FindFirstByType<PlayerController>();
            if (player == null) return;
            player.Revive();
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        });
    }

    public void Restart()
    {
        AdsManager.Instance.MaybeShowInterstitial("game_over");
        SceneLoader.ReloadGame();
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
