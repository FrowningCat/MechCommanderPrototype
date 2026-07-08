using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Tooltip("Точное имя геймплейной сцены, регистр важен")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    public void OnPlayClicked()
    {
        Debug.Log($"[MainMenuController] Loading scene: {gameplaySceneName}");

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnQuitClicked()
    {
        Debug.Log("[MainMenuController] Quit requested");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}