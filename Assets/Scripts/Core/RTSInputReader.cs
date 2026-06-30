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
    private bool isAttackMoveMode;

    private InputSystem_Actions inputActions;

    private readonly List<UnitSelectable> selectedUnits = new();
    private readonly List<MechMovement> selectedMechs = new();
    private readonly List<MechCombat> selectedCombatUnits = new();

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
        isAttackMoveMode = true;
        Debug.Log("Attack Move Mode");
    }

    private void Update()
    {
        if (!isDragging)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (selectionBoxUI != null)
            selectionBoxUI.UpdateSelection(mousePos);
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
        }
        else if (!addSelect)
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

        Ray ray =
            mainCamera.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, enemyLayer))
        {
            Health target =
                hit.collider.GetComponentInParent<Health>();

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
            bool attackMovePressed = inputActions.RTS.AttackMove.IsPressed();

            if (isAttackMoveMode || attackMovePressed)
            {
                foreach (MechCombat combat in selectedCombatUnits)
                {
                    combat.SetAttackMoveDestination(hit.point);
                }

                isAttackMoveMode = false;
            }
            else
            {
                foreach (MechCombat combat in selectedCombatUnits)
                {
                    combat.ClearTarget();
                }
            }

            MoveSelectedMechsInFormation(hit.point);
        }
    }

    private void MoveSelectedMechsInFormation(Vector3 centerPoint)
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

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int column = i % columns;

            float xOffset = (column - (columns - 1) * 0.5f) * formationSpacing;
            float zOffset = (row - (rows - 1) * 0.5f) * formationSpacing;

            Vector3 targetPosition =
                centerPoint +
                moveRight * xOffset -
                moveForward * zOffset;

            targetPosition = GetNearestNavMeshPoint(targetPosition);

            selectedMechs[i].MoveTo(targetPosition);
        }
    }

    private Vector3 GetNearestNavMeshPoint(Vector3 point)
    {
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            return hit.position;

        return point;
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

        selectedUnits.RemoveAt(index);
        selectedMechs.RemoveAt(index);

        if (index < selectedCombatUnits.Count)
        {
            selectedCombatUnits.RemoveAt(index);
        }
    }

    public void ClearSelection()
    {
        foreach (UnitSelectable unit in selectedUnits)
            unit.Deselect();

        selectedUnits.Clear();
        selectedMechs.Clear();
        selectedCombatUnits.Clear();
    }

    public IReadOnlyList<UnitSelectable> GetSelectedUnits()
    {
        return selectedUnits;
    }
}