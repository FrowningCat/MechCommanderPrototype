using UnityEngine;

public class UnitSelectable : MonoBehaviour
{
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private Sprite portrait;

    public Sprite Portrait => portrait;

    // Lets MechLoadoutApplier assign the right portrait for the model actually
    // active on this shared Mech prefab, since a single static sprite can't
    // represent all 4 selectable player models.
    public void SetPortrait(Sprite newPortrait)
    {
        portrait = newPortrait;
    }

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