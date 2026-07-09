using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionResultUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MissionController missionController;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    [Header("Scenes")]
    [Tooltip("Точное имя сцены главного меню, регистр важен")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Audio")]
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip defeatSound;

    private void OnEnable()
    {
        if (missionController != null)
            missionController.OnMissionEnded += HandleMissionEnded;
    }

    private void OnDisable()
    {
        if (missionController != null)
            missionController.OnMissionEnded -= HandleMissionEnded;
    }

    private void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void HandleMissionEnded(MissionState state)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
            resultText.text = state == MissionState.Victory ? "Победа" : "Поражение";

        if (AudioManager.Instance != null)
        {
            AudioClip clip = state == MissionState.Victory ? victorySound : defeatSound;
            AudioManager.Instance.PlayUiSound(clip);
        }
    }

    public void OnMainMenuClicked()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnRestartClicked()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}