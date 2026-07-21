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

    // Centers the camera directly over `point` at the game's standard viewing angle (the fixed
    // downward-forward pitch set on this transform, unaffected by zoom — HandleZoom only ever
    // changes height). The camera doesn't look straight down, so simply matching X/Z to the point
    // leaves it looking at ground well in front of the point; the camera has to sit back by
    // (height above the point) * tan(pitch) for the point to land in the center of the view. This
    // replaces a hardcoded "-20" that only happened to roughly work at the scene's default height
    // (25) and didn't account for the point's own Y, so it visibly drifted off-center whenever the
    // camera wasn't at that exact height (e.g. after zooming) or the target sat above/below y=0.
    public void FocusOnPoint(Vector3 point)
    {
        Vector3 newPosition = transform.position;

        float pitchRad = transform.eulerAngles.x * Mathf.Deg2Rad;
        float heightAbovePoint = newPosition.y - point.y;
        float forwardOffset = heightAbovePoint * Mathf.Tan(pitchRad);

        newPosition.x = Mathf.Clamp(point.x, boundMinX, boundMaxX);
        newPosition.z = Mathf.Clamp(point.z - forwardOffset, boundMinZ, boundMaxZ);

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

    public float MaxHeight => maxHeight;

    // Worst-case horizontal distance from the camera's XZ position to the ground point visible at
    // the very TOP edge of the screen, at max zoom-out (maxHeight). Used by LevelGenerator to size
    // the water/ground backdrop so it can never be outrun by panning + zooming out, which would
    // otherwise expose the skybox (see Stage 34 grey-horizon bug — Camera.clearFlags is Skybox, and
    // the procedural skybox's GroundColor is a flat grey that shows wherever the water mesh runs
    // out before the view ray does).
    // Only the vertical half-FOV matters for this ray's angle below horizontal: pitch is the only
    // rotation this rig ever applies (no yaw/roll), so any ray inside the frustum keeps the same
    // downward angle as the vertical-edge ray that defines the top of the screen — horizontal
    // (left/right) offset doesn't change elevation.
    public float ComputeMaxGroundViewDistance()
    {
        Camera cam = GetComponent<Camera>();
        float verticalHalfFov = cam != null ? cam.fieldOfView * 0.5f : 30f;
        float pitchDeg = transform.eulerAngles.x;
        float rayAngleBelowHorizontal = pitchDeg - verticalHalfFov;

        // A near-horizontal (or upward) top-of-frustum ray never hits the ground plane at a finite
        // distance — fall back to a large-but-finite value rather than Infinity.
        if (rayAngleBelowHorizontal <= 1f)
            return maxHeight * 200f;

        return maxHeight / Mathf.Tan(rayAngleBelowHorizontal * Mathf.Deg2Rad);
    }
}