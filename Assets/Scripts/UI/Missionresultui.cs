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
        // Mid-run victory: LevelUpgradeUI takes over (upgrade pick, then straight to the next
        // level) instead of the MainMenu/Restart panel below. Falls through to the normal panel
        // if there's no MechLoadoutData (e.g. the gameplay scene launched directly, with no run).
        if (state == MissionState.Victory && MechLoadoutData.Instance != null)
            return;

        if (state == MissionState.Defeat && MechLoadoutData.Instance != null)
            MechLoadoutData.Instance.ResetRun();

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
        AdsManager.Instance.ShowInterstitial(() => SceneManager.LoadScene(mainMenuSceneName));
    }

    public void OnRestartClicked()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        AdsManager.Instance.ShowInterstitial(() => SceneManager.LoadScene(currentSceneName));
    }
}