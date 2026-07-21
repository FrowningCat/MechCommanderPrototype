using UnityEngine;

// Central place for the Stage 33 run-based progression constants and formulas. Deliberately kept
// separate from WeaponBalance, which models weapon type/fire mode trade-offs and has nothing to
// do with level-over-level growth.
public static class RunProgression
{
    // Enemy Health/damage grow +10% per run level, linear/additive: level N multiplier =
    // 1 + (N-1) * 0.10 (level 1 = x1.0, level 2 = x1.10, level 3 = x1.20, ...). Chosen over
    // compound growth (1.10^(N-1)) because this is an endless run with no planned level cap —
    // compound growth would spiral fast (level 15 ~= x3.5, level 25 ~= x9.8) while the player's
    // own gear only grows by one flat +15% pick per level, so a compounding enemy curve makes the
    // run unwinnable past a fairly low level almost by construction. Linear keeps each level's
    // difficulty step constant and predictable, and matches the (N-1)*10% example from the design
    // spec directly.
    public const float EnemyStatIncrementPerLevel = 0.10f;

    public static float GetEnemyStatMultiplier(int runLevel)
    {
        int level = Mathf.Max(1, runLevel);
        return 1f + (level - 1) * EnemyStatIncrementPerLevel;
    }

    // A single pick on the between-level upgrade screen grants a flat +15% to the chosen stat's
    // running multiplier (see MechLoadoutData.RunHealthMultiplier etc.), stacking multiplicatively
    // pick over pick. LevelUpgradeUI previews the result the exact same way it applies it
    // (currentEffectiveValue * (1 + increment)), so the preview can never drift from the outcome.
    public const float PlayerUpgradeIncrement = 0.15f;
}
