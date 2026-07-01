using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Transform fill;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
    [SerializeField] private float smoothSpeed = 8f;

    private Camera cam;
    private Image fillImage;
    private float currentFill = 1f;

    private void Awake()
    {
        cam = Camera.main;

        if (fill != null)
            fillImage = fill.GetComponent<Image>();
    }

    private void LateUpdate()
    {
        if (health == null || fill == null)
            return;

        transform.position = health.transform.position + offset;

        if (cam != null)
            transform.forward = cam.transform.forward;

        float targetFill = Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);

        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * smoothSpeed);

        Vector3 scale = fill.localScale;
        scale.x = currentFill;
        fill.localScale = scale;

        if (fillImage != null)
        {
            if (targetFill > 0.5f)
                fillImage.color = Color.green;
            else if (targetFill > 0.25f)
                fillImage.color = Color.yellow;
            else
                fillImage.color = Color.red;
        }
    }
}