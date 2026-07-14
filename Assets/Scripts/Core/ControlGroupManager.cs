using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControlGroupManager : MonoBehaviour
{
    [SerializeField] private RTSInputReader rtsInputReader;
    [SerializeField] private RTSCameraController cameraController;

    [Header("Double Tap")]
    [SerializeField] private float doubleTapTime = 0.35f;

    private InputSystem_Actions inputActions;

    private readonly Dictionary<int, List<UnitSelectable>> controlGroups = new();

    private System.Action<InputAction.CallbackContext>[] selectGroupHandlers;
    private System.Action<InputAction.CallbackContext>[] setGroupHandlers;

    private int lastSelectedGroup = -1;
    private float lastSelectTime;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();

        if (rtsInputReader == null)
            rtsInputReader = GetComponent<RTSInputReader>();

        if (cameraController == null)
            cameraController = FindFirstObjectByType<RTSCameraController>();

        BuildHandlers();
    }

    private void BuildHandlers()
    {
        selectGroupHandlers = new System.Action<InputAction.CallbackContext>[10];
        setGroupHandlers = new System.Action<InputAction.CallbackContext>[10];

        for (int i = 1; i <= 9; i++)
        {
            int groupNumber = i;
            selectGroupHandlers[i] = _ => SelectGroup(groupNumber);
            setGroupHandlers[i] = _ => SetGroup(groupNumber);
        }
    }

    private void OnEnable()
    {
        inputActions.RTSGroups.Enable();

        inputActions.RTSGroups.Group1.performed += selectGroupHandlers[1];
        inputActions.RTSGroups.Group2.performed += selectGroupHandlers[2];
        inputActions.RTSGroups.Group3.performed += selectGroupHandlers[3];
        inputActions.RTSGroups.Group4.performed += selectGroupHandlers[4];
        inputActions.RTSGroups.Group5.performed += selectGroupHandlers[5];
        inputActions.RTSGroups.Group6.performed += selectGroupHandlers[6];
        inputActions.RTSGroups.Group7.performed += selectGroupHandlers[7];
        inputActions.RTSGroups.Group8.performed += selectGroupHandlers[8];
        inputActions.RTSGroups.Group9.performed += selectGroupHandlers[9];

        inputActions.RTSGroups.SetGroup1.performed += setGroupHandlers[1];
        inputActions.RTSGroups.SetGroup2.performed += setGroupHandlers[2];
        inputActions.RTSGroups.SetGroup3.performed += setGroupHandlers[3];
        inputActions.RTSGroups.SetGroup4.performed += setGroupHandlers[4];
        inputActions.RTSGroups.SetGroup5.performed += setGroupHandlers[5];
        inputActions.RTSGroups.SetGroup6.performed += setGroupHandlers[6];
        inputActions.RTSGroups.SetGroup7.performed += setGroupHandlers[7];
        inputActions.RTSGroups.SetGroup8.performed += setGroupHandlers[8];
        inputActions.RTSGroups.SetGroup9.performed += setGroupHandlers[9];
    }

    private void OnDisable()
    {
        inputActions.RTSGroups.Group1.performed -= selectGroupHandlers[1];
        inputActions.RTSGroups.Group2.performed -= selectGroupHandlers[2];
        inputActions.RTSGroups.Group3.performed -= selectGroupHandlers[3];
        inputActions.RTSGroups.Group4.performed -= selectGroupHandlers[4];
        inputActions.RTSGroups.Group5.performed -= selectGroupHandlers[5];
        inputActions.RTSGroups.Group6.performed -= selectGroupHandlers[6];
        inputActions.RTSGroups.Group7.performed -= selectGroupHandlers[7];
        inputActions.RTSGroups.Group8.performed -= selectGroupHandlers[8];
        inputActions.RTSGroups.Group9.performed -= selectGroupHandlers[9];

        inputActions.RTSGroups.SetGroup1.performed -= setGroupHandlers[1];
        inputActions.RTSGroups.SetGroup2.performed -= setGroupHandlers[2];
        inputActions.RTSGroups.SetGroup3.performed -= setGroupHandlers[3];
        inputActions.RTSGroups.SetGroup4.performed -= setGroupHandlers[4];
        inputActions.RTSGroups.SetGroup5.performed -= setGroupHandlers[5];
        inputActions.RTSGroups.SetGroup6.performed -= setGroupHandlers[6];
        inputActions.RTSGroups.SetGroup7.performed -= setGroupHandlers[7];
        inputActions.RTSGroups.SetGroup8.performed -= setGroupHandlers[8];
        inputActions.RTSGroups.SetGroup9.performed -= setGroupHandlers[9];

        inputActions.RTSGroups.Disable();
    }

    private void SetGroup(int groupNumber)
    {
        IReadOnlyList<UnitSelectable> selectedUnits = rtsInputReader.GetSelectedUnits();

        if (selectedUnits.Count == 0)
            return;

        controlGroups[groupNumber] = new List<UnitSelectable>(selectedUnits);

        Debug.Log($"Saved control group {groupNumber}: {selectedUnits.Count} unit(s)");
    }

    private void SelectGroup(int groupNumber)
    {
        if (!controlGroups.TryGetValue(groupNumber, out List<UnitSelectable> group))
            return;

        bool isDoubleTap =
            lastSelectedGroup == groupNumber &&
            Time.time - lastSelectTime <= doubleTapTime;

        lastSelectedGroup = groupNumber;
        lastSelectTime = Time.time;

        rtsInputReader.ClearSelection();

        for (int i = group.Count - 1; i >= 0; i--)
        {
            UnitSelectable unit = group[i];

            if (unit == null)
            {
                group.RemoveAt(i);
                continue;
            }

            rtsInputReader.AddUnit(unit);
        }

        Debug.Log($"Selected control group {groupNumber}: {group.Count} unit(s)");

        if (isDoubleTap)
            FocusCameraOnGroup(group);
    }

    private void FocusCameraOnGroup(List<UnitSelectable> group)
    {
        if (cameraController == null || group.Count == 0)
            return;

        Vector3 center = Vector3.zero;
        int validUnits = 0;

        foreach (UnitSelectable unit in group)
        {
            if (unit == null)
                continue;

            center += unit.transform.position;
            validUnits++;
        }

        if (validUnits == 0)
            return;

        center /= validUnits;

        cameraController.FocusOnPoint(center);
    }
}