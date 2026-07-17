using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Tooltip("Точное имя сцены настройки меха перед миссией, регистр важен")]
    [SerializeField] private string mechSetupSceneName = "MechSetup";

    [Header("Audio")]
    [SerializeField] private AudioClip menuMusic;

    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(menuMusic);
    }

    public void OnPlayClicked()
    {
        Debug.Log($"[MainMenuController] Loading scene: {mechSetupSceneName}");

        SceneManager.LoadScene(mechSetupSceneName);
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