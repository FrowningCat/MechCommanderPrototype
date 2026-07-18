using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using TMPro;

public class IntroCutsceneController : MonoBehaviour
{
    [System.Serializable]
    private struct BriefingLine
    {
        public float startTime;
        public float endTime;
        [TextArea] public string text;
    }

    [Header("Timeline")]
    [SerializeField] private PlayableDirector director;

    [Header("Combat pause (background fights shouldn't resolve during the cutscene)")]
    [SerializeField] private MissionController missionController;

    [Header("Player control (disabled during cutscene)")]
    [SerializeField] private RTSInputReader inputReader;
    [SerializeField] private RTSCameraController rtsCameraController;
    [SerializeField] private Camera mainCamera;

    [Header("Player mech intro walk")]
    [SerializeField] private MechMovement playerMech;
    [SerializeField] private MechCombat playerMechCombat;
    [SerializeField] private TerrainSpeedController playerTerrainSpeedController;
    [SerializeField] private Vector3 introDestination = new Vector3(0f, 1f, 3f);
    [SerializeField] private float introWalkSpeed = 2.1f;

    [Header("Briefing UI")]
    [SerializeField] private CanvasGroup briefingCanvasGroup;
    [SerializeField] private TMP_Text briefingText;
    [SerializeField] private float fadeDuration = 0.5f;

    [SerializeField]
    private BriefingLine[] briefingLines =
    {
        new BriefingLine { startTime = 0f, endTime = 6f, text = "Разведка сообщает: город пал под ударом вражеской артиллерии." },
        new BriefingLine { startTime = 6f, endTime = 12f, text = "Уцелевших нет — только руины и брошенная техника." },
        new BriefingLine { startTime = 12f, endTime = 17f, text = "Двигайся к точке сбора. Возможны уцелевшие враги." },
        new BriefingLine { startTime = 17f, endTime = 21f, text = "Противник где-то впереди. Будь готов к бою." },
    };

    private NavMeshAgent playerAgent;
    private float originalAgentSpeed;
    private bool hasEnded;
    private int activeLineIndex = -1;

    private EnemyAI[] pausedEnemyAIs;
    private MechCombat[] pausedMechCombats;

    // Allows LevelGenerator to point the intro walk at the procedurally generated destination
    // instead of the hardcoded scene value. Must be called before Start() (i.e. from another
    // component's Awake()), since Start() is what issues the MoveTo order.
    public void SetIntroDestination(Vector3 destination)
    {
        introDestination = destination;
    }

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerMech != null)
            playerAgent = playerMech.GetComponent<NavMeshAgent>();

        if (briefingCanvasGroup != null)
            briefingCanvasGroup.alpha = 0f;

        if (briefingText != null)
            briefingText.text = string.Empty;
    }

    private void Start()
    {
        if (director == null)
            return;

        if (inputReader != null)
            inputReader.enabled = false;

        if (rtsCameraController != null)
            rtsCameraController.enabled = false;

        if (mainCamera != null)
            mainCamera.enabled = false;

        // TerrainSpeedController overwrites agent.speed every frame from terrain/heat
        // multipliers, so it must be disabled while we drive a fixed cinematic walk speed.
        if (playerTerrainSpeedController != null)
            playerTerrainSpeedController.enabled = false;

        if (playerAgent != null)
        {
            originalAgentSpeed = playerAgent.speed;
            playerAgent.speed = introWalkSpeed;
        }

        // MechCombat.Update() runs an idle/return-to-anchor loop that calls
        // agent.isStopped = true every frame when there is no active order, which
        // would immediately cancel a destination set directly via MechMovement.
        // Issuing the walk through MechCombat's own Move order keeps it moving.
        if (playerMechCombat != null)
            playerMechCombat.MoveTo(introDestination);
        else if (playerMech != null)
            playerMech.MoveTo(introDestination);

        // EnemyAI/MechCombat/Weapon keep running in the background regardless of player input
        // being disabled above — without this, a fight can start and resolve (even end the
        // mission) while the player has no control yet, and Time.timeScale = 0 isn't usable
        // here since Timeline/Animator playback depends on it. Freeze both explicitly instead,
        // and un-freeze in the same place input/camera control is handed back.
        pausedEnemyAIs = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        pausedMechCombats = FindObjectsByType<MechCombat>(FindObjectsSortMode.None);

        foreach (EnemyAI enemyAI in pausedEnemyAIs)
            enemyAI.enabled = false;

        foreach (MechCombat mechCombat in pausedMechCombats)
            mechCombat.enabled = false;

        if (missionController != null)
            missionController.SetMissionActive(false);

        director.stopped += OnCutsceneStopped;
        director.time = 0;
        director.Play();
    }

    private void Update()
    {
        if (hasEnded || director == null)
            return;

        UpdateBriefingText((float)director.time);

        if (SkipWasPressed())
            director.Stop();
    }

    private bool SkipWasPressed()
    {
        if (Keyboard.current == null)
            return false;

        return Keyboard.current.escapeKey.wasPressedThisFrame ||
               Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    private void UpdateBriefingText(float time)
    {
        if (briefingCanvasGroup == null || briefingText == null)
            return;

        int lineIndex = -1;

        for (int i = 0; i < briefingLines.Length; i++)
        {
            if (time >= briefingLines[i].startTime && time < briefingLines[i].endTime)
            {
                lineIndex = i;
                break;
            }
        }

        if (lineIndex != activeLineIndex)
        {
            activeLineIndex = lineIndex;
            briefingText.text = lineIndex >= 0 ? briefingLines[lineIndex].text : string.Empty;
        }

        if (lineIndex < 0)
        {
            briefingCanvasGroup.alpha = 0f;
            return;
        }

        BriefingLine line = briefingLines[lineIndex];
        float timeIntoLine = time - line.startTime;
        float timeToLineEnd = line.endTime - time;

        float fadeIn = fadeDuration <= 0f ? 1f : Mathf.Clamp01(timeIntoLine / fadeDuration);
        float fadeOut = fadeDuration <= 0f ? 1f : Mathf.Clamp01(timeToLineEnd / fadeDuration);

        briefingCanvasGroup.alpha = Mathf.Min(fadeIn, fadeOut);
    }

    private void OnCutsceneStopped(PlayableDirector stoppedDirector)
    {
        EndCutscene();
    }

    private void EndCutscene()
    {
        if (hasEnded)
            return;

        hasEnded = true;

        if (director != null)
            director.stopped -= OnCutsceneStopped;

        if (inputReader != null)
            inputReader.enabled = true;

        if (rtsCameraController != null)
            rtsCameraController.enabled = true;

        if (mainCamera != null)
            mainCamera.enabled = true;

        if (playerAgent != null)
            playerAgent.speed = originalAgentSpeed;

        if (playerTerrainSpeedController != null)
            playerTerrainSpeedController.enabled = true;

        if (pausedEnemyAIs != null)
        {
            foreach (EnemyAI enemyAI in pausedEnemyAIs)
            {
                if (enemyAI != null)
                    enemyAI.enabled = true;
            }
        }

        if (pausedMechCombats != null)
        {
            foreach (MechCombat mechCombat in pausedMechCombats)
            {
                if (mechCombat != null)
                    mechCombat.enabled = true;
            }
        }

        if (missionController != null)
            missionController.SetMissionActive(true);

        if (briefingCanvasGroup != null)
            briefingCanvasGroup.alpha = 0f;

        if (briefingText != null)
            briefingText.text = string.Empty;
    }
}
