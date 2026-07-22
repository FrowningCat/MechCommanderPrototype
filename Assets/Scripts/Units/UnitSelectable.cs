using UnityEngine;

public class UnitSelectable : MonoBehaviour
{
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private Sprite portrait;
    [SerializeField] private string displayName;

    public Sprite Portrait => portrait;

    // Falls back to the GameObject's own name (stripped of Instantiate's "(Clone)"/"(1)"
    // suffixes) so enemies without an explicit DisplayName still show something readable
    // in UnitInfoPanel instead of raw "Enemy_Variant2(Clone)".
    public string DisplayName => string.IsNullOrEmpty(displayName) ? StripCloneSuffix(gameObject.name) : displayName;

    // Lets MechLoadoutApplier assign the right portrait for the model actually
    // active on this shared Mech prefab, since a single static sprite can't
    // represent all 4 selectable player models.
    public void SetPortrait(Sprite newPortrait)
    {
        portrait = newPortrait;
    }

    public void SetDisplayName(string name)
    {
        displayName = name;
    }

    private static string StripCloneSuffix(string rawName)
    {
        int parenIndex = rawName.IndexOf('(');
        return parenIndex >= 0 ? rawName.Substring(0, parenIndex).Trim() : rawName;
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