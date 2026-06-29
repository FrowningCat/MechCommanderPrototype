using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RTSInputReader : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask unitLayer;
    [SerializeField] private SelectionBoxUI selectionBoxUI;
    [SerializeField] private float dragThreshold = 10f;

    private InputSystem_Actions inputActions;

    private readonly List<UnitSelectable> selectedUnits = new();
    private readonly List<MechMovement> selectedMechs = new();

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
    }

    private void OnDisable()
    {
        inputActions.RTS.Select.started -= OnSelectStarted;
        inputActions.RTS.Select.canceled -= OnSelectCanceled;

        inputActions.RTS.Command.performed -= OnCommand;

        inputActions.RTS.Disable();
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

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            foreach (MechMovement mech in selectedMechs)
            {
                mech.MoveTo(hit.point);
            }
        }
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

    private void AddUnit(UnitSelectable unit)
    {
        if (selectedUnits.Contains(unit))
            return;

        MechMovement mechMovement = unit.GetComponent<MechMovement>();

        if (mechMovement == null)
            return;

        selectedUnits.Add(unit);
        selectedMechs.Add(mechMovement);

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
    }

    private void ClearSelection()
    {
        foreach (UnitSelectable unit in selectedUnits)
            unit.Deselect();

        selectedUnits.Clear();
        selectedMechs.Clear();
    }
}