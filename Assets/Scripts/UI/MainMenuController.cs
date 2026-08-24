using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button survivalButton;

    private void OnEnable()
    {
        GameManager.Instance.OnStateChanged += HandleState;
        startButton.onClick.AddListener(() => StartClicked(GameMode.Campaign));
        survivalButton.onClick.AddListener(() => StartClicked(GameMode.Survival));
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnStateChanged -= HandleState;
        startButton.onClick.RemoveAllListeners();
        survivalButton.onClick.RemoveAllListeners();
    }

    private void HandleState(GameState state)
    {
        panel.SetActive(state == GameState.Menu);
    }

    private void StartClicked(GameMode mode) => GameManager.Instance.StartRun(mode);
}
