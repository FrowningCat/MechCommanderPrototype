using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class OutroCutsceneController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector outroDirector;

    [Header("References")]
    [SerializeField] private MissionController missionController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera outroCamera;

    private bool hasEnded;
    private bool isPlaying;

    private void OnEnable()
    {
        if (missionController != null)
            missionController.OnMissionEnded += HandleMissionEnded;
    }

    private void OnDisable()
    {
        if (missionController != null)
            missionController.OnMissionEnded -= HandleMissionEnded;

        if (outroDirector != null)
            outroDirector.stopped -= OnOutroStopped;
    }

    private void HandleMissionEnded(MissionState state)
    {
        if (outroDirector == null || outroCamera == null)
            return;

        hasEnded = false;
        isPlaying = true;

        if (mainCamera != null)
            mainCamera.enabled = false;

        outroCamera.gameObject.SetActive(true);

        outroDirector.stopped += OnOutroStopped;
        outroDirector.time = 0;
        outroDirector.Play();
    }

    private void Update()
    {
        if (!isPlaying || hasEnded)
            return;

        if (Keyboard.current != null &&
            (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            outroDirector.Stop();
        }
    }

    private void OnOutroStopped(PlayableDirector stoppedDirector)
    {
        if (hasEnded)
            return;

        hasEnded = true;
        isPlaying = false;

        outroDirector.stopped -= OnOutroStopped;

        if (outroCamera != null)
            outroCamera.gameObject.SetActive(false);

        if (mainCamera != null)
            mainCamera.enabled = true;
    }
}
