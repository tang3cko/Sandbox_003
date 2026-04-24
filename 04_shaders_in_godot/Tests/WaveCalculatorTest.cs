using Xunit;

namespace SwarmSurvivor.Tests;

public class WaveCalculatorTest
{
    [Fact]
    public void CreateInitial_SetsDefaults()
    {
        var state = WaveCalculator.CreateInitial();

        Assert.Equal(0, state.WaveNumber);
        Assert.Equal(0, state.TotalKills);
        Assert.False(state.IsGameOver);
        Assert.False(state.IsWaveActive);
        Assert.False(state.IsVictory);
    }

    [Fact]
    public void StartNextWave_IncrementsWaveNumber()
    {
        var state = WaveCalculator.CreateInitial();
        var result = WaveCalculator.StartNextWave(state, 50, 1.0f);

        Assert.Equal(1, result.State.WaveNumber);
        Assert.True(result.State.IsWaveActive);
    }

    [Fact]
    public void StartNextWave_SetsCorrectEnemyCount()
    {
        var state = WaveCalculator.CreateInitial();
        var result = WaveCalculator.StartNextWave(state, 50, 1.0f);

        Assert.Equal(50, result.EnemyCount);
        Assert.Equal(50, result.State.EnemiesRemaining);
        Assert.Equal(50, result.State.TotalEnemiesInWave);
    }

    [Fact]
    public void StartNextWave_ScalesWithGrowthRate()
    {
        var state = WaveCalculator.CreateInitial();
        var wave1 = WaveCalculator.StartNextWave(state, 50, 1.0f);
        var wave2 = WaveCalculator.StartNextWave(wave1.State, 50, 1.0f);

        Assert.True(wave2.EnemyCount > wave1.EnemyCount);
    }

    [Fact]
    public void StartNextWave_ResetsSpawnedCount()
    {
        var state = WaveCalculator.CreateInitial();
        var result = WaveCalculator.StartNextWave(state, 50, 1.0f);

        Assert.Equal(0, result.State.EnemiesSpawned);
    }

    [Fact]
    public void EnemySpawned_IncrementsCount()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state, 50, 1.0f);
        var spawned = WaveCalculator.EnemySpawned(wave.State);

        Assert.Equal(1, spawned.EnemiesSpawned);
    }

    [Fact]
    public void EnemyDefeated_DecrementsRemaining()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state, 50, 1.0f);
        var result = WaveCalculator.EnemyDefeated(wave.State);

        Assert.Equal(49, result.State.EnemiesRemaining);
        Assert.False(result.IsWaveComplete);
    }

    [Fact]
    public void EnemyDefeated_IncrementsTotalKills()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state, 50, 1.0f);
        var result = WaveCalculator.EnemyDefeated(wave.State);

        Assert.Equal(1, result.State.TotalKills);
    }

    [Fact]
    public void EnemyDefeated_CompletesWaveAtZero()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state, 1, 1.0f);
        var result = WaveCalculator.EnemyDefeated(wave.State);

        Assert.Equal(0, result.State.EnemiesRemaining);
        Assert.True(result.IsWaveComplete);
        Assert.False(result.State.IsWaveActive);
    }

    [Fact]
    public void EnemyDefeated_TriggersVictoryOnFinalWave()
    {
        var state = WaveCalculator.CreateInitial();

        for (int w = 0; w < WaveCalculator.MaxWaves; w++)
        {
            var wave = WaveCalculator.StartNextWave(state, 1, 0f);
            var defeated = WaveCalculator.EnemyDefeated(wave.State);
            state = defeated.State;
        }

        Assert.True(state.IsVictory);
        Assert.Equal(WaveCalculator.MaxWaves, state.WaveNumber);
    }

    [Fact]
    public void EnemyDefeated_NoVictoryBeforeFinalWave()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state, 1, 1.0f);
        var result = WaveCalculator.EnemyDefeated(wave.State);

        Assert.False(result.State.IsVictory);
    }

    [Fact]
    public void EnemyDefeated_ClampsRemainingToZero()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state, 1, 1.0f);
        var first = WaveCalculator.EnemyDefeated(wave.State);
        var second = WaveCalculator.EnemyDefeated(first.State);

        Assert.Equal(0, second.State.EnemiesRemaining);
    }

    [Fact]
    public void PlayerDefeated_SetsGameOver()
    {
        var state = WaveCalculator.CreateInitial();
        var result = WaveCalculator.PlayerDefeated(state);

        Assert.True(result.State.IsGameOver);
    }

    [Fact]
    public void PlayerDefeated_PreservesKillCount()
    {
        var state = WaveCalculator.CreateInitial();
        var wave = WaveCalculator.StartNextWave(state, 50, 1.0f);
        var killed = WaveCalculator.EnemyDefeated(wave.State);
        var result = WaveCalculator.PlayerDefeated(killed.State);

        Assert.Equal(1, result.State.TotalKills);
    }

    [Fact]
    public void GetEnemyCountForWave_ReturnsBaseForWave1()
    {
        int count = WaveCalculator.GetEnemyCountForWave(1, 50, 1.0f);

        Assert.Equal(50, count);
    }

    [Fact]
    public void GetEnemyCountForWave_ScalesCorrectly()
    {
        int wave3 = WaveCalculator.GetEnemyCountForWave(3, 50, 1.0f);

        Assert.Equal(150, wave3);
    }

    [Fact]
    public void GetEnemyCountForWave_ReturnsZeroForInvalidWave()
    {
        Assert.Equal(0, WaveCalculator.GetEnemyCountForWave(0, 50, 1.0f));
        Assert.Equal(0, WaveCalculator.GetEnemyCountForWave(-1, 50, 1.0f));
    }

    [Fact]
    public void GetEnemyCountForWave_MinimumOne()
    {
        int count = WaveCalculator.GetEnemyCountForWave(1, 0, 1.0f);

        Assert.True(count >= 1);
    }

    [Fact]
    public void GetWaveDifficulty_ReturnsOneForWave1()
    {
        Assert.Equal(1.0f, WaveCalculator.GetWaveDifficulty(1));
    }

    [Fact]
    public void GetWaveDifficulty_IncreasesPerWave()
    {
        float d1 = WaveCalculator.GetWaveDifficulty(1);
        float d2 = WaveCalculator.GetWaveDifficulty(2);
        float d3 = WaveCalculator.GetWaveDifficulty(3);

        Assert.True(d3 > d2);
        Assert.True(d2 > d1);
    }

    [Fact]
    public void GetSpawnInterval_DecreasesWithWave()
    {
        float i1 = WaveCalculator.GetSpawnInterval(1, 0.5f);
        float i3 = WaveCalculator.GetSpawnInterval(3, 0.5f);

        Assert.True(i3 < i1);
    }

    [Fact]
    public void GetSpawnInterval_HasMinimum()
    {
        float interval = WaveCalculator.GetSpawnInterval(999, 0.5f);

        Assert.True(interval >= 0.05f);
    }
}
