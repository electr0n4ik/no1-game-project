using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField] private TMP_Text timerLabel;
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private Image xpBar;
    [SerializeField] private PlayerGrowth growth;

    private void Start()
    {
        growth.OnLevelUp += _ => RefreshXp();
        growth.OnXpChanged += RefreshXp;
        RefreshXp();
    }

    private void OnEnable()
    {
        GameManager.Instance.OnRunTimer += UpdateTimer;
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnRunTimer -= UpdateTimer;
    }

    private void UpdateTimer(float t)
    {
        int minutes = (int)(t / 60f);
        int seconds = (int)(t % 60f);
        timerLabel.text = $"{minutes:0}:{seconds:00}";
    }

    private void RefreshXp()
    {
        levelLabel.text = $"LVL {growth.Level}";
        xpBar.fillAmount = Mathf.Clamp01(growth.Xp / growth.XpNeeded);
    }
}
