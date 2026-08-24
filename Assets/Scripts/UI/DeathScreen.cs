using Rubilovo.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text statsLabel;
    [SerializeField] private TMP_Text meatLabel;
    [SerializeField] private Button reviveButton;
    [SerializeField] private Button doubleMeatButton;
    [SerializeField] private Button restartButton;

    private long pendingMeat;
    private bool doubled;
    private int levelReached;

    private void OnEnable()
    {
        GameManager.Instance.OnStateChanged += HandleState;
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnStateChanged -= HandleState;
    }

    private void HandleState(GameState state)
    {
        if (state != GameState.Dead)
        {
            panel.SetActive(false);
            return;
        }

        panel.SetActive(true);
        bool victory = GameManager.Instance.LastRunWasVictory;
        titleLabel.text = victory ? "ПОБЕДА" : "ПОРАЖЕНИЕ";

        RunTracker tracker = RunTracker.Instance;
        float t = GameManager.Instance.RunTime;
        levelReached = FindFirstByType<PlayerGrowth>() != null ? FindFirstByType<PlayerGrowth>().Level : 1;
        int kills = tracker != null ? tracker.TotalKills : 0;

        statsLabel.text = $"Время {Mathf.FloorToInt(t / 60f)}:{Mathf.FloorToInt(t % 60f):00}   " +
                          $"Уровень {levelReached}\n" +
                          $"Убийства: {kills}   Элиты: {(tracker != null ? tracker.ElitesKilled : 0)}   " +
                          $"Боссы: {(tracker != null ? tracker.BossesKilled : 0)}";

        SaveSystem.Data.bestLevel = Mathf.Max(SaveSystem.Data.bestLevel, levelReached);
        SaveSystem.RecordBest(t, kills, levelReached);

        pendingMeat = Economy.MeatForRunFinal(
            kills,
            tracker != null ? tracker.ElitesKilled : 0,
            tracker != null ? tracker.BossesKilled : 0,
            SaveSystem.RunIndexOfNext());
        doubled = false;
        SaveSystem.AddMeat(pendingMeat);

        meatLabel.text = $"🍖 +{pendingMeat}";
        doubleMeatButton.gameObject.SetActive(pendingMeat > 0 && AdsManager.Instance.RewardedAvailable);
        doubleMeatButton.interactable = true;

        reviveButton.gameObject.SetActive(!victory && AdsManager.Instance.RewardedAvailable);
        reviveButton.interactable = true;
    }

    public void OnReviveClicked()
    {
        reviveButton.interactable = false;
        GameManager.Instance.TryRevive();
    }

    public void OnDoubleMeatClicked()
    {
        if (doubled) return;
        doubleMeatButton.interactable = false;
        AdsManager.Instance.TryShowRewarded(AdPlacements.DoubleRunMeat, ok =>
        {
            if (!ok) { doubleMeatButton.interactable = true; return; }
            doubled = true;
            SaveSystem.AddMeat(pendingMeat);
            meatLabel.text = $"🍖 +{pendingMeat * 2}";
            doubleMeatButton.gameObject.SetActive(false);
        });
    }
}
