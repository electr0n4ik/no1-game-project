using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button startButton;

    private void OnEnable()
    {
        GameManager.Instance.OnStateChanged += HandleState;
        startButton.onClick.AddListener(StartClicked);
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnStateChanged -= HandleState;
        startButton.onClick.RemoveListener(StartClicked);
    }

    private void HandleState(GameState state)
    {
        panel.SetActive(state == GameState.Menu);
    }

    private void StartClicked()
    {
        GameManager.Instance.StartRun();
    }
}
