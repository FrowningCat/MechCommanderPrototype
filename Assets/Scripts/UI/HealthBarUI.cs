using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Transform fill;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
    [SerializeField] private float smoothSpeed = 8f;

    [Tooltip("Extra gap above the model's actual top (from its renderer bounds) before the bar sits.")]
    [SerializeField] private float heightMargin = 0.3f;

    private Camera cam;
    private Image fillImage;
    private float currentFill = 1f;

    private void Awake()
    {
        cam = Camera.main;

        if (fill != null)
            fillImage = fill.GetComponent<Image>();

        RecalculateOffsetFromModelBounds();
    }

    // Runs after MechLoadoutApplier (execution order -1000) has already picked the active
    // model variant, so only the currently-active renderers are measured — different models
    // (different heights) each get a correctly placed bar instead of one hardcoded offset.
    private void RecalculateOffsetFromModelBounds()
    {
        if (health == null)
            return;

        Renderer[] renderers = health.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return;

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combined.Encapsulate(renderers[i].bounds);

        float topAboveOrigin = combined.max.y - health.transform.position.y;
        offset = new Vector3(offset.x, topAboveOrigin + heightMargin, offset.z);
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