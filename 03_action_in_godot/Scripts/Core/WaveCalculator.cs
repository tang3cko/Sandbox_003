namespace ArenaSurvivor;

public struct WaveState
{
    public int WaveNumber;
    public int EnemiesRemaining;
    public int EnemiesSpawned;
    public int TotalEnemiesInWave;
    public int Score;
    public bool IsGameOver;
    public bool IsWaveActive;
    public float TimeBetweenWaves;
}

public readonly record struct WaveAdvanceResult(WaveState State, int EnemyCount);

public readonly record struct EnemyDefeatedResult(WaveState State, int ScoreGained, bool IsWaveComplete);

public readonly record struct PlayerDefeatedResult(WaveState State);

public static class WaveCalculator
{
    public static WaveState CreateInitial()
    {
        return new WaveState
        {
            WaveNumber = 0,
            Score = 0,
            EnemiesRemaining = 0,
            EnemiesSpawned = 0,
            TotalEnemiesInWave = 0,
            IsGameOver = false,
            IsWaveActive = false,
            TimeBetweenWaves = 0f,
        };
    }

    public static WaveAdvanceResult StartNextWave(WaveState state)
    {
        int nextWave = state.WaveNumber + 1;
        int enemyCount = 3 + nextWave * 2;

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

    public static EnemyDefeatedResult EnemyDefeated(WaveState state, int baseScore)
    {
        int scoreGained = baseScore * state.WaveNumber;
        int remaining = state.EnemiesRemaining - 1;
        bool isWaveComplete = remaining <= 0;

        var newState = state;
        newState.EnemiesRemaining = remaining;
        newState.Score = state.Score + scoreGained;
        if (isWaveComplete)
        {
            newState.IsWaveActive = false;
        }

        return new EnemyDefeatedResult(newState, scoreGained, isWaveComplete);
    }

    public static PlayerDefeatedResult PlayerDefeated(WaveState state)
    {
        var newState = state;
        newState.IsGameOver = true;
        return new PlayerDefeatedResult(newState);
    }

    public static float GetWaveDifficulty(int waveNumber)
    {
        float difficulty = 1.0f + (waveNumber - 1) * 0.15f;
        return difficulty < 1.0f ? 1.0f : difficulty;
    }
}
