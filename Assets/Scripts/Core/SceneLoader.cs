using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static void ReloadGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
