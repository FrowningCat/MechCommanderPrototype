using UnityEngine;
using UnityEngine.InputSystem;

public class RTSCameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 25f;

    [Header("Mouse Edge Scrolling")]
    [SerializeField] private bool useEdgeScrolling = true;
    [SerializeField] private float edgeSize = 20f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 300f;
    [SerializeField] private float minHeight = 10f;
    [SerializeField] private float maxHeight = 60f;

    // Left permissive by default so the camera behaves exactly as before (unbounded) in any
    // scene/context that never calls SetBounds — LevelGenerator is what supplies real limits,
    // derived from the actual baked NavMesh bounding box rather than a symmetric half-extent
    // (generated maps aren't square).
    private float boundMinX = Mathf.NegativeInfinity;
    private float boundMaxX = Mathf.Infinity;
    private float boundMinZ = Mathf.NegativeInfinity;
    private float boundMaxZ = Mathf.Infinity;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.RTS.Enable();
    }

    private void OnDisable()
    {
        inputActions.RTS.Disable();
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    private void HandleMovement()
    {
        Vector2 keyboardInput = inputActions.RTS.CameraMove.ReadValue<Vector2>();
        Vector2 edgeInput = GetMouseEdgeInput();

        Vector2 finalInput = keyboardInput + edgeInput;

        if (finalInput.sqrMagnitude > 1f)
            finalInput.Normalize();

        Vector3 moveDirection = new Vector3(finalInput.x, 0f, finalInput.y);

        Vector3 newPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;
        newPosition.x = Mathf.Clamp(newPosition.x, boundMinX, boundMaxX);
        newPosition.z = Mathf.Clamp(newPosition.z, boundMinZ, boundMaxZ);

        transform.position = newPosition;
    }

    private Vector2 GetMouseEdgeInput()
    {
        if (!useEdgeScrolling || Mouse.current == null)
            return Vector2.zero;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 input = Vector2.zero;

        if (mousePosition.x <= edgeSize)
            input.x = -1f;
        else if (mousePosition.x >= Screen.width - edgeSize)
            input.x = 1f;

        if (mousePosition.y <= edgeSize)
            input.y = -1f;
        else if (mousePosition.y >= Screen.height - edgeSize)
            input.y = 1f;

        return input;
    }

    private void HandleZoom()
    {
        float zoomInput = inputActions.RTS.CameraZoom.ReadValue<float>();

        if (Mathf.Abs(zoomInput) < 0.01f)
            return;

        Vector3 pos = transform.position;
        pos.y -= zoomInput * zoomSpeed * Time.deltaTime;
        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);

        transform.position = pos;
    }

    public void FocusOnPoint(Vector3 point)
    {
        Vector3 newPosition = transform.position;

        newPosition.x = Mathf.Clamp(point.x, boundMinX, boundMaxX);
        newPosition.z = Mathf.Clamp(point.z - 20f, boundMinZ, boundMaxZ);

        transform.position = newPosition;
    }

    // Called by LevelGenerator once the mission's actual walkable footprint is known (the real
    // baked NavMesh bounding box, not a symmetric half-extent — generated maps aren't square),
    // so the camera can't pan past the generated level's real ground edge.
    public void SetBounds(float minX, float maxX, float minZ, float maxZ)
    {
        boundMinX = minX;
        boundMaxX = maxX;
        boundMinZ = minZ;
        boundMaxZ = maxZ;
    }
}