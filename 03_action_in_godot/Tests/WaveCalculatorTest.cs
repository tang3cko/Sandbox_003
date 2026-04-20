using Xunit;

namespace ArenaSurvivor.Tests;

public class WaveCalculatorTest
{
    [Fact]
    public void CreateInitial_ReturnsWaveNumberZero()
    {
        var state = WaveCalculator.CreateInitial();

        Assert.Equal(0, state.WaveNumber);
    }

    [Fact]
    public void CreateInitial_ReturnsScoreZero()
    {
        var state = WaveCalculator.CreateInitial();

        Assert.Equal(0, state.Score);
    }

    [Fact]
    public void CreateInitial_ReturnsGameOverFalse()
    {
        var state = WaveCalculator.CreateInitial();

        Assert.False(state.IsGameOver);
    }

    [Fact]
    public void CreateInitial_ReturnsWaveActiveFalse()
    {
        var state = WaveCalculator.CreateInitial();

        Assert.False(state.IsWaveActive);
    }

    [Fact]
    public void CreateInitial_ReturnsEnemiesRemainingZero()
    {
        var state = WaveCalculator.CreateInitial();

        Assert.Equal(0, state.EnemiesRemaining);
    }

    [Fact]
    public void StartNextWave_IncrementsWaveNumber()
    {
        var state = WaveCalculator.CreateInitial();

        var result = WaveCalculator.StartNextWave(state);

        Assert.Equal(1, result.State.WaveNumber);
    }

    [Fact]
    public void StartNextWave_SetsIsWaveActiveTrue()
    {
        var state = WaveCalculator.CreateInitial();

        var result = WaveCalculator.StartNextWave(state);

        Assert.True(result.State.IsWaveActive);
    }

    [Fact]
    public void StartNextWave_Wave1_Returns5Enemies()
    {
        var state = WaveCalculator.CreateInitial();

        var result = WaveCalculator.StartNextWave(state);

        Assert.Equal(5, result.EnemyCount);
    }

    [Fact]
    public void StartNextWave_Wave2_Returns7Enemies()
    {
        var state = WaveCalculator.CreateInitial();
        var wave1 = WaveCalculator.StartNextWave(state);

        var result = WaveCalculator.StartNextWave(wave1.State);

        Assert.Equal(7, result.EnemyCount);
    }

    [Fact]
    public void StartNextWave_Wave3_Returns9Enemies()
    {
        var state = WaveCalculator.CreateInitial();
        var wave1 = WaveCalculator.StartNextWave(state);
        var wave2 = WaveCalculator.StartNextWave(wave1.State);

        var result = WaveCalculator.StartNextWave(wave2.State);

        Assert.Equal(9, result.EnemyCount);
    }

    [Fact]
    public void StartNextWave_SetsEnemiesRemainingToEnemyCount()
    {
        var state = WaveCalculator.CreateInitial();

        var result = WaveCalculator.StartNextWave(state);

        Assert.Equal(result.EnemyCount, result.State.EnemiesRemaining);
    }

    [Fact]
    public void StartNextWave_SetsTotalEnemiesInWaveToEnemyCount()
    {
        var state = WaveCalculator.CreateInitial();

        var result = WaveCalculator.StartNextWave(state);

        Assert.Equal(result.EnemyCount, result.State.TotalEnemiesInWave);
    }

    [Fact]
    public void StartNextWave_ResetsEnemiesSpawnedToZero()
    {
        var state = WaveCalculator.CreateInitial();

        var result = WaveCalculator.StartNextWave(state);

        Assert.Equal(0, result.State.EnemiesSpawned);
    }

    [Fact]
    public void EnemySpawned_IncrementsEnemiesSpawned()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state);

        var result = WaveCalculator.EnemySpawned(wave.State);

        Assert.Equal(1, result.EnemiesSpawned);
    }

    [Fact]
    public void EnemySpawned_CalledTwice_IncrementsToTwo()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state);

        var first = WaveCalculator.EnemySpawned(wave.State);
        var second = WaveCalculator.EnemySpawned(first);

        Assert.Equal(2, second.EnemiesSpawned);
    }

    [Fact]
    public void EnemyDefeated_DecrementsEnemiesRemaining()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state);

        var result = WaveCalculator.EnemyDefeated(wave.State, 10);

        Assert.Equal(wave.State.EnemiesRemaining - 1, result.State.EnemiesRemaining);
    }

    [Fact]
    public void EnemyDefeated_AddsScoreWithWaveMultiplier()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state);
        int baseScore = 10;

        var result = WaveCalculator.EnemyDefeated(wave.State, baseScore);

        Assert.Equal(baseScore * wave.State.WaveNumber, result.ScoreGained);
    }

    [Fact]
    public void EnemyDefeated_AccumulatesScore()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state);
        int baseScore = 10;

        var first = WaveCalculator.EnemyDefeated(wave.State, baseScore);
        var second = WaveCalculator.EnemyDefeated(first.State, baseScore);

        Assert.Equal(baseScore * wave.State.WaveNumber * 2, second.State.Score);
    }

    [Fact]
    public void EnemyDefeated_NotLastEnemy_IsWaveCompleteFalse()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state);

        var result = WaveCalculator.EnemyDefeated(wave.State, 10);

        Assert.False(result.IsWaveComplete);
    }

    [Fact]
    public void EnemyDefeated_LastEnemy_IsWaveCompleteTrue()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state);

        var current = wave.State;
        EnemyDefeatedResult result = default;
        for (int i = 0; i < wave.EnemyCount; i++)
        {
            result = WaveCalculator.EnemyDefeated(current, 10);
            current = result.State;
        }

        Assert.True(result.IsWaveComplete);
    }

    [Fact]
    public void EnemyDefeated_LastEnemy_SetsIsWaveActiveFalse()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state);

        var current = wave.State;
        EnemyDefeatedResult result = default;
        for (int i = 0; i < wave.EnemyCount; i++)
        {
            result = WaveCalculator.EnemyDefeated(current, 10);
            current = result.State;
        }

        Assert.False(result.State.IsWaveActive);
    }

    [Fact]
    public void PlayerDefeated_SetsIsGameOverTrue()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state);

        var result = WaveCalculator.PlayerDefeated(wave.State);

        Assert.True(result.State.IsGameOver);
    }

    [Fact]
    public void PlayerDefeated_PreservesScore()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state);
        var defeated = WaveCalculator.EnemyDefeated(wave.State, 10);

        var result = WaveCalculator.PlayerDefeated(defeated.State);

        Assert.Equal(defeated.State.Score, result.State.Score);
    }

    [Fact]
    public void GetWaveDifficulty_Wave1_Returns1Point0()
    {
        var difficulty = WaveCalculator.GetWaveDifficulty(1);

        Assert.Equal(1.0f, difficulty);
    }

    [Fact]
    public void GetWaveDifficulty_Wave2_Returns1Point15()
    {
        var difficulty = WaveCalculator.GetWaveDifficulty(2);

        Assert.Equal(1.15f, difficulty);
    }

    [Fact]
    public void GetWaveDifficulty_Wave5_Returns1Point6()
    {
        var difficulty = WaveCalculator.GetWaveDifficulty(5);

        Assert.Equal(1.6f, difficulty);
    }

    [Fact]
    public void GetWaveDifficulty_Wave0_ReturnsMinimum1Point0()
    {
        var difficulty = WaveCalculator.GetWaveDifficulty(0);

        Assert.Equal(1.0f, difficulty);
    }
}
