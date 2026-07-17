using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text messageText;

    private bool autoHideActive;
    private float hideTimer;

    private void Start()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
            return;

        if (AnyKeyOrClickPressed())
        {
            Hide();
            return;
        }

        if (!autoHideActive)
            return;

        hideTimer -= Time.deltaTime;

        if (hideTimer <= 0f)
            Hide();
    }

    public void ShowMessage(string speakerName, string message, float autoHideDelay)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speakerName));
        }

        if (messageText != null)
            messageText.text = message;

        autoHideActive = autoHideDelay > 0f;
        hideTimer = autoHideDelay;
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        autoHideActive = false;
    }

    private bool AnyKeyOrClickPressed()
    {
        bool keyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

        bool clickPressed = Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame);

        return keyPressed || clickPressed;
    }
}
