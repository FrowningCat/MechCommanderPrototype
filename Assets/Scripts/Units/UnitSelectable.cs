using UnityEngine;

public class UnitSelectable : MonoBehaviour
{
    [SerializeField] private GameObject selectionIndicator;

    public void Select()
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(true);
    }

    public void Deselect()
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }
}