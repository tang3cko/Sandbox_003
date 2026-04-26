using System;

namespace SwarmSurvivor;

public struct PlayerProgression
{
    public int Level;
    public int CurrentXP;
    public int XPToNextLevel;
}

public readonly record struct XPGainResult(PlayerProgression State, bool DidLevelUp);

public readonly record struct UpgradeChoice(int Id, string Name, string Description);

public static class UpgradeCalculator
{
    public static PlayerProgression CreateInitial(int xpToFirstLevel)
    {
        return new PlayerProgression
        {
            Level = 1,
            CurrentXP = 0,
            XPToNextLevel = Math.Max(1, xpToFirstLevel),
        };
    }

    public static XPGainResult GainXP(PlayerProgression state, int amount, int baseXP, float growthFactor)
    {
        if (amount <= 0)
        {
            return new XPGainResult(state, false);
        }

        var newState = state;
        newState.CurrentXP = state.CurrentXP + amount;

        if (newState.CurrentXP >= newState.XPToNextLevel)
        {
            newState.CurrentXP -= newState.XPToNextLevel;
            newState.Level += 1;
            newState.XPToNextLevel = CalculateXPForLevel(newState.Level, baseXP, growthFactor);
            return new XPGainResult(newState, true);
        }

        return new XPGainResult(newState, false);
    }

    public static int CalculateXPForLevel(int level, int baseXP, float growthFactor)
    {
        if (level <= 1) return Math.Max(1, baseXP);
        float xp = baseXP * MathF.Pow(growthFactor, level - 1);
        return Math.Max(1, (int)MathF.Round(xp));
    }

    public static UpgradeChoice[] GetRandomChoices(
        UpgradeChoice[] available, int count, int seed)
    {
        if (available == null || available.Length == 0)
        {
            return Array.Empty<UpgradeChoice>();
        }

        int resultCount = Math.Min(count, available.Length);
        var result = new UpgradeChoice[resultCount];
        var indices = new int[available.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = i;
        }

        var rng = new Random(seed);
        for (int i = 0; i < resultCount; i++)
        {
            int remaining = indices.Length - i;
            int pick = rng.Next(remaining);
            int swapIndex = i + pick;

            (indices[i], indices[swapIndex]) = (indices[swapIndex], indices[i]);
            result[i] = available[indices[i]];
        }

        return result;
    }

    public static float GetXPProgress(PlayerProgression state)
    {
        if (state.XPToNextLevel <= 0) return 1f;
        return Math.Clamp((float)state.CurrentXP / state.XPToNextLevel, 0f, 1f);
    }
}
