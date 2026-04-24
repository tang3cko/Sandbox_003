using System;

namespace SwarmSurvivor;

public struct WaveState
{
    public int WaveNumber;
    public int EnemiesRemaining;
    public int EnemiesSpawned;
    public int TotalEnemiesInWave;
    public int TotalKills;
    public bool IsGameOver;
    public bool IsWaveActive;
    public bool IsVictory;
}

public readonly record struct WaveAdvanceResult(WaveState State, int EnemyCount);

public readonly record struct EnemyDefeatedResult(WaveState State, bool IsWaveComplete);

public readonly record struct PlayerDefeatedResult(WaveState State);

public static class WaveCalculator
{
    public const int MaxWaves = 15;

    public static WaveState CreateInitial()
    {
        return new WaveState
        {
            WaveNumber = 0,
            EnemiesRemaining = 0,
            EnemiesSpawned = 0,
            TotalEnemiesInWave = 0,
            TotalKills = 0,
            IsGameOver = false,
            IsWaveActive = false,
            IsVictory = false,
        };
    }

    public static WaveAdvanceResult StartNextWave(WaveState state, int baseCount, float growthRate)
    {
        int nextWave = state.WaveNumber + 1;
        int enemyCount = GetEnemyCountForWave(nextWave, baseCount, growthRate);

        var newState = state;
        newState.WaveNumber = nextWave;
        newState.IsWaveActive = true;
        newState.EnemiesRemaining = enemyCount;
        newState.TotalEnemiesInWave = enemyCount;
        newState.EnemiesSpawned = 0;

        return new WaveAdvanceResult(newState, enemyCount);
    }

    public static WaveState EnemySpawned(WaveState state)
    {
        var newState = state;
        newState.EnemiesSpawned = state.EnemiesSpawned + 1;
        return newState;
    }

    public static EnemyDefeatedResult EnemyDefeated(WaveState state)
    {
        int remaining = state.EnemiesRemaining - 1;
        bool isWaveComplete = remaining <= 0;

        var newState = state;
        newState.EnemiesRemaining = Math.Max(0, remaining);
        newState.TotalKills = state.TotalKills + 1;

        if (isWaveComplete)
        {
            newState.IsWaveActive = false;
            if (newState.WaveNumber >= MaxWaves)
            {
                newState.IsVictory = true;
            }
        }

        return new EnemyDefeatedResult(newState, isWaveComplete);
    }

    public static PlayerDefeatedResult PlayerDefeated(WaveState state)
    {
        var newState = state;
        newState.IsGameOver = true;
        return new PlayerDefeatedResult(newState);
    }

    public static int GetEnemyCountForWave(int waveNumber, int baseCount, float growthRate)
    {
        if (waveNumber <= 0) return 0;
        float count = baseCount * (1f + (waveNumber - 1) * growthRate);
        return Math.Max(1, (int)MathF.Round(count));
    }

    public static float GetWaveDifficulty(int waveNumber)
    {
        float difficulty = 1.0f + (waveNumber - 1) * 0.2f;
        return difficulty < 1.0f ? 1.0f : difficulty;
    }

    public static float GetSpawnInterval(int waveNumber, float baseInterval)
    {
        float interval = baseInterval / GetWaveDifficulty(waveNumber);
        return Math.Max(0.05f, interval);
    }
}
