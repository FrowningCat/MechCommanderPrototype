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

    private int lastSelectedGroup = -1;
    private float lastSelectTime;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();

        if (rtsInputReader == null)
            rtsInputReader = GetComponent<RTSInputReader>();

        if (cameraController == null)
            cameraController = FindFirstObjectByType<RTSCameraController>();
    }

    private void OnEnable()
    {
        inputActions.RTSGroups.Enable();

        inputActions.RTSGroups.Group1.performed += _ => SelectGroup(1);
        inputActions.RTSGroups.Group2.performed += _ => SelectGroup(2);
        inputActions.RTSGroups.Group3.performed += _ => SelectGroup(3);
        inputActions.RTSGroups.Group4.performed += _ => SelectGroup(4);
        inputActions.RTSGroups.Group5.performed += _ => SelectGroup(5);
        inputActions.RTSGroups.Group6.performed += _ => SelectGroup(6);
        inputActions.RTSGroups.Group7.performed += _ => SelectGroup(7);
        inputActions.RTSGroups.Group8.performed += _ => SelectGroup(8);
        inputActions.RTSGroups.Group9.performed += _ => SelectGroup(9);

        inputActions.RTSGroups.SetGroup1.performed += _ => SetGroup(1);
        inputActions.RTSGroups.SetGroup2.performed += _ => SetGroup(2);
        inputActions.RTSGroups.SetGroup3.performed += _ => SetGroup(3);
        inputActions.RTSGroups.SetGroup4.performed += _ => SetGroup(4);
        inputActions.RTSGroups.SetGroup5.performed += _ => SetGroup(5);
        inputActions.RTSGroups.SetGroup6.performed += _ => SetGroup(6);
        inputActions.RTSGroups.SetGroup7.performed += _ => SetGroup(7);
        inputActions.RTSGroups.SetGroup8.performed += _ => SetGroup(8);
        inputActions.RTSGroups.SetGroup9.performed += _ => SetGroup(9);
    }

    private void OnDisable()
    {
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