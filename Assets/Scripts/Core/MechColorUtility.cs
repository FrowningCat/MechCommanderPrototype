using UnityEngine;

public static class MechColorUtility
{
    private const string PlayerColorMaterialName = "Main";

    public static void ApplyPlayerColor(GameObject model, Color color)
    {
        if (model == null)
            return;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (IsPlayerColorSlot(materials[i]))
                {
                    materials[i].color = color;
                    changed = true;
                }
            }

            if (changed)
                renderer.materials = materials;
        }
    }

    private static bool IsPlayerColorSlot(Material material)
    {
        if (material == null)
            return false;

        Color color = material.color;
        bool isGreenish = color.g > color.r * 1.3f && color.g > color.b * 1.3f;

        if (isGreenish)
            return true;

        string cleanName = material.name.Replace("(Instance)", string.Empty).Trim();
        return cleanName.Equals(PlayerColorMaterialName, System.StringComparison.OrdinalIgnoreCase);
    }
}
