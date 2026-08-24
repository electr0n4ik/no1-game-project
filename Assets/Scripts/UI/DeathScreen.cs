using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button reviveButton;
    [SerializeField] private TMP_Text resultLabel;

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
        bool dead = state == GameState.Dead;
        panel.SetActive(dead);
        if (!dead) return;
        resultLabel.text = $"Вы выжили {GameManager.Instance.RunTime:0} сек\nУровень {FindFirstByType<PlayerGrowth>().Level}";
        reviveButton.gameObject.SetActive(true);
        reviveButton.interactable = true;
    }

    public void OnReviveClicked()
    {
        reviveButton.interactable = false;
        GameManager.Instance.TryRevive();
    }
}
