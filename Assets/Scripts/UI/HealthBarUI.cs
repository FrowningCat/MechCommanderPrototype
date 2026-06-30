using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Transform fill;

    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (health == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = health.transform.position + offset;
        transform.forward = cam.transform.forward;

        float hp = (float)health.CurrentHealth / health.MaxHealth;

        Vector3 scale = fill.localScale;
        scale.x = Mathf.Clamp01(hp);
        fill.localScale = scale;
    }
}