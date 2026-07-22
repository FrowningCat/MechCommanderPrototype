using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class RTSInputReader : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask unitLayer;
    [SerializeField] private SelectionBoxUI selectionBoxUI;

    [Header("Selection")]
    [SerializeField] private float dragThreshold = 10f;

    [Header("Formation")]
    [SerializeField] private float formationSpacing = 3f;

    private enum PendingCommandMode
    {
        None,
        AttackMove,
        Patrol,
        Guard
    }

    private enum MoveOrderType
    {
        Move,
        AttackMove
    }

    private PendingCommandMode pendingCommand = PendingCommandMode.None;

    private InputSystem_Actions inputActions;

    private readonly List<UnitSelectable> selectedUnits = new();
    private readonly List<MechMovement> selectedMechs = new();
    private readonly List<MechCombat> selectedCombatUnits = new();

    // Enemy units clicked for inspection only (HUD/portrait display) — never
    // participate in move/attack orders, so they're kept out of selectedUnits
    // and its parallel movement/combat lists entirely. At most one entry.
    private readonly List<UnitSelectable> inspectedEnemyUnits = new();

    // Parallel to selectedUnits/selectedMechs (same index, may contain null
    // for units without MechCombat) — avoids GetComponent lookups in hot paths.
    private readonly List<MechCombat> selectedMechCombats = new();

    private bool isDragging;
    private Vector2 dragStartPos;
    private Vector2 dragEndPos;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        inputActions.RTS.Enable();

        inputActions.RTS.Select.started += OnSelectStarted;
        inputActions.RTS.Select.canceled += OnSelectCanceled;
        inputActions.RTS.Command.performed += OnCommand;
        inputActions.RTS.AttackMove.performed += OnAttackMove;
    }

    private void OnDisable()
    {
        inputActions.RTS.Select.started -= OnSelectStarted;
        inputActions.RTS.Select.canceled -= OnSelectCanceled;
        inputActions.RTS.Command.performed -= OnCommand;
        inputActions.RTS.AttackMove.performed -= OnAttackMove;

        inputActions.RTS.Disable();
    }

    private void OnAttackMove(InputAction.CallbackContext context)
    {
        pendingCommand = PendingCommandMode.AttackMove;
        Debug.Log("Attack Move Mode");
    }

    private void Update()
    {
        HandleOrderHotkeys();

        if (!isDragging)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (selectionBoxUI != null)
            selectionBoxUI.UpdateSelection(mousePos);
    }

    private void HandleOrderHotkeys()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            pendingCommand = PendingCommandMode.Patrol;
            Debug.Log("Patrol Mode");
        }

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            pendingCommand = PendingCommandMode.Guard;
            Debug.Log("Guard Mode");
        }

        if (Keyboard.current.hKey.wasPressedThisFrame)
            ExecuteHoldPosition();

        if (Keyboard.current.xKey.wasPressedThisFrame)
            CycleStance();
    }

    private void OnSelectStarted(InputAction.CallbackContext context)
    {
        isDragging = true;
        dragStartPos = Mouse.current.position.ReadValue();

        if (selectionBoxUI != null)
            selectionBoxUI.BeginSelection(dragStartPos);
    }

    private void OnSelectCanceled(InputAction.CallbackContext context)
    {
        isDragging = false;
        dragEndPos = Mouse.current.position.ReadValue();

        if (selectionBoxUI != null)
            selectionBoxUI.EndSelection();

        bool addSelect = inputActions.RTS.AddSelect.IsPressed();

        if (Vector2.Distance(dragStartPos, dragEndPos) <= dragThreshold)
            HandleSingleClickSelection(addSelect);
        else
            HandleBoxSelection(addSelect);
    }

    private void HandleSingleClickSelection(bool addSelect)
    {
        Ray ray = mainCamera.ScreenPointToRay(dragEndPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, unitLayer))
        {
            UnitSelectable unit = hit.collider.GetComponentInParent<UnitSelectable>();

            if (addSelect)
                ToggleUnit(unit);
            else
                SelectOnly(unit);

            return;
        }

        if (!addSelect && Physics.Raycast(ray, out RaycastHit enemyHit, 500f, enemyLayer))
        {
            SelectEnemyForInspection(enemyHit.collider.GetComponentInParent<UnitSelectable>());
            return;
        }

        if (!addSelect)
        {
            ClearSelection();
        }
    }

    private void HandleBoxSelection(bool addSelect)
    {
        if (!addSelect)
            ClearSelection();

        Rect selectionRect = GetScreenRect(dragStartPos, dragEndPos);

        UnitSelectable[] allUnits = FindObjectsByType<UnitSelectable>(
            FindObjectsSortMode.None
        );

        foreach (UnitSelectable unit in allUnits)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(unit.transform.position);

            if (screenPos.z < 0f)
                continue;

            if (selectionRect.Contains(screenPos, true))
                AddUnit(unit);
        }
    }

    private Rect GetScreenRect(Vector2 start, Vector2 end)
    {
        float xMin = Mathf.Min(start.x, end.x);
        float yMin = Mathf.Min(start.y, end.y);
        float width = Mathf.Abs(start.x - end.x);
        float height = Mathf.Abs(start.y - end.y);

        return new Rect(xMin, yMin, width, height);
    }

    private void OnCommand(InputAction.CallbackContext context)
    {
        if (selectedMechs.Count == 0)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        PendingCommandMode commandToExecute = pendingCommand;
        pendingCommand = PendingCommandMode.None;

        if (commandToExecute == PendingCommandMode.Guard)
        {
            ExecuteGuardCommand(ray);
            return;
        }

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, enemyLayer))
        {
            Health target = hit.collider.GetComponentInParent<Health>();

            if (target != null)
            {
                foreach (MechCombat combat in selectedCombatUnits)
                {
                    combat.SetTarget(target);
                }
            }

            return;
        }

        if (Physics.Raycast(ray, out hit, 500f, groundLayer))
        {
            if (commandToExecute == PendingCommandMode.Patrol)
            {
                ExecutePatrolCommand(hit.point);
                return;
            }

            bool attackMovePressed = inputActions.RTS.AttackMove.IsPressed();
            bool attackMove = commandToExecute == PendingCommandMode.AttackMove || attackMovePressed;

            if (!attackMove)
            {
                foreach (MechCombat combat in selectedCombatUnits)
                {
                    combat.Stop();
                }
            }

            MoveSelectedMechsInFormation(
                hit.point,
                attackMove ? MoveOrderType.AttackMove : MoveOrderType.Move
            );
        }
    }

    private void ExecuteGuardCommand(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, unitLayer))
            return;

        UnitSelectable allyUnit = hit.collider.GetComponentInParent<UnitSelectable>();

        if (allyUnit == null)
            return;

        foreach (MechCombat combat in selectedCombatUnits)
        {
            if (allyUnit.transform != combat.transform)
                combat.GuardTarget(allyUnit.transform);
        }
    }

    private void ExecuteHoldPosition()
    {
        pendingCommand = PendingCommandMode.None;

        if (selectedCombatUnits.Count == 0)
            return;

        foreach (MechCombat combat in selectedCombatUnits)
        {
            combat.HoldPosition();
        }

        Debug.Log("Hold Position");
    }

    private void ExecutePatrolCommand(Vector3 centerPoint)
    {
        if (selectedMechs.Count == 0)
            return;

        Vector3[] positions = ComputeFormationPositions(centerPoint);

        for (int i = 0; i < selectedMechCombats.Count; i++)
        {
            selectedMechCombats[i]?.SetPatrol(positions[i]);
        }
    }

    private void CycleStance()
    {
        pendingCommand = PendingCommandMode.None;

        if (selectedCombatUnits.Count == 0)
            return;

        foreach (MechCombat combat in selectedCombatUnits)
        {
            combat.SetStance(NextStance(combat.Stance));
        }

        Debug.Log($"Stance: {selectedCombatUnits[0].Stance}");
    }

    private static UnitStance NextStance(UnitStance current)
    {
        return current switch
        {
            UnitStance.Passive => UnitStance.Defensive,
            UnitStance.Defensive => UnitStance.Aggressive,
            UnitStance.Aggressive => UnitStance.Passive,
            _ => UnitStance.Defensive,
        };
    }

    private void MoveSelectedMechsInFormation(Vector3 centerPoint, MoveOrderType orderType)
    {
        Vector3[] positions = ComputeFormationPositions(centerPoint);

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            MechCombat combat = selectedMechCombats[i];

            if (combat != null)
            {
                if (orderType == MoveOrderType.AttackMove)
                    combat.AttackMoveTo(positions[i]);
                else
                    combat.MoveTo(positions[i]);
            }
            else
            {
                selectedMechs[i].MoveTo(positions[i]);
            }
        }
    }

    private Vector3[] ComputeFormationPositions(Vector3 centerPoint)
    {
        int count = selectedMechs.Count;

        int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / columns);

        Vector3 groupCenter = Vector3.zero;

        foreach (MechMovement mech in selectedMechs)
            groupCenter += mech.transform.position;

        groupCenter /= count;

        Vector3 moveForward = centerPoint - groupCenter;
        moveForward.y = 0f;

        if (moveForward.sqrMagnitude < 0.01f)
        {
            moveForward = mainCamera.transform.forward;
            moveForward.y = 0f;
        }

        moveForward.Normalize();

        Vector3 moveRight = Vector3.Cross(Vector3.up, moveForward).normalized;

        // NavMeshAgent radius on the Mech prefab (~3.16) is bigger than the old fixed
        // 3f spacing, so grid slots could overlap even on open ground. Widen spacing to
        // guarantee non-overlapping agents before NavMesh clamping ever comes into play.
        float agentRadius = selectedMechs.Count > 0 ? selectedMechs[0].AgentRadius : 0.5f;
        float minSeparation = agentRadius * 2f + 0.5f;
        float spacing = Mathf.Max(formationSpacing, minSeparation);

        Vector3[] positions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int column = i % columns;

            float xOffset = (column - (columns - 1) * 0.5f) * spacing;
            float zOffset = (row - (rows - 1) * 0.5f) * spacing;

            Vector3 targetPosition =
                centerPoint +
                moveRight * xOffset -
                moveForward * zOffset;

            Vector3 clamped = GetNearestNavMeshPoint(targetPosition);

            positions[i] = ResolveOverlap(clamped, positions, i, minSeparation, moveForward, moveRight);
        }

        return positions;
    }

    // Narrow paths surrounded by water mean several formation slots can all get clamped
    // by NavMesh.SamplePosition onto the same (or a nearly identical) nearest point on the
    // walkable strip. When that happens, walk the candidate outward — preferring along the
    // path's own direction (moveForward) over across it (moveRight), since across is far more
    // likely to run straight into water — until it clears every already-placed position by
    // at least minSeparation, or we give up and accept the closest point found.
    private Vector3 ResolveOverlap(
        Vector3 candidate,
        Vector3[] placedPositions,
        int placedCount,
        float minSeparation,
        Vector3 alongDir,
        Vector3 acrossDir)
    {
        if (!IsTooClose(candidate, placedPositions, placedCount, minSeparation))
            return candidate;

        Vector3[] directions =
        {
            alongDir, -alongDir,
            acrossDir, -acrossDir,
            (alongDir + acrossDir).normalized, (alongDir - acrossDir).normalized,
            (-alongDir + acrossDir).normalized, (-alongDir - acrossDir).normalized,
        };

        const int maxSteps = 6;

        for (int step = 1; step <= maxSteps; step++)
        {
            float stepDistance = minSeparation * step;

            foreach (Vector3 direction in directions)
            {
                Vector3 attempt = candidate + direction * stepDistance;

                if (!NavMesh.SamplePosition(attempt, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                    continue;

                if (!IsTooClose(hit.position, placedPositions, placedCount, minSeparation))
                    return hit.position;
            }
        }

        return candidate;
    }

    private static bool IsTooClose(Vector3 point, Vector3[] placedPositions, int placedCount, float minSeparation)
    {
        for (int i = 0; i < placedCount; i++)
        {
            if (Vector3.Distance(point, placedPositions[i]) < minSeparation)
                return true;
        }

        return false;
    }

    private Vector3 GetNearestNavMeshPoint(Vector3 point)
    {
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            return hit.position;

        return point;
    }

    private void SelectEnemyForInspection(UnitSelectable enemyUnit)
    {
        ClearSelection();

        if (enemyUnit == null)
            return;

        inspectedEnemyUnits.Add(enemyUnit);
        enemyUnit.Select();
    }

    private void SelectOnly(UnitSelectable unit)
    {
        ClearSelection();

        if (unit == null)
            return;

        AddUnit(unit);
    }

    private void ToggleUnit(UnitSelectable unit)
    {
        if (unit == null)
            return;

        if (selectedUnits.Contains(unit))
            RemoveUnit(unit);
        else
            AddUnit(unit);
    }

    public void AddUnit(UnitSelectable unit)
    {
        if (selectedUnits.Contains(unit))
            return;

        MechMovement mechMovement = unit.GetComponent<MechMovement>();
        MechCombat combat = unit.GetComponent<MechCombat>();

        if (mechMovement == null)
            return;

        selectedUnits.Add(unit);
        selectedMechs.Add(mechMovement);
        selectedMechCombats.Add(combat);

        if (combat != null)
        {
            selectedCombatUnits.Add(combat);
        }

        unit.Select();
    }

    private void RemoveUnit(UnitSelectable unit)
    {
        int index = selectedUnits.IndexOf(unit);

        if (index < 0)
            return;

        selectedUnits[index].Deselect();

        MechCombat combat = selectedMechCombats[index];

        selectedUnits.RemoveAt(index);
        selectedMechs.RemoveAt(index);
        selectedMechCombats.RemoveAt(index);

        if (combat != null)
        {
            selectedCombatUnits.Remove(combat);
        }
    }

    public void ClearSelection()
    {
        foreach (UnitSelectable unit in selectedUnits)
            unit.Deselect();

        selectedUnits.Clear();
        selectedMechs.Clear();
        selectedMechCombats.Clear();
        selectedCombatUnits.Clear();

        foreach (UnitSelectable unit in inspectedEnemyUnits)
            unit.Deselect();

        inspectedEnemyUnits.Clear();
    }

    public IReadOnlyList<UnitSelectable> GetSelectedUnits()
    {
        return selectedUnits.Count > 0 ? selectedUnits : inspectedEnemyUnits;
    }
}