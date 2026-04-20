namespace TowerDefense.Tests;

public class GameStateCalculatorTest
{
    [Fact]
    public void CreateInitial_SetsGoldAndLives()
    {
        var state = GameStateCalculator.CreateInitial(200, 20);

        Assert.Equal(200, state.Gold);
        Assert.Equal(20, state.Lives);
        Assert.Equal(0, state.Score);
        Assert.Equal(0, state.WaveNumber);
        Assert.False(state.IsGameOver);
    }

    [Fact]
    public void EnemyKilled_AddsScoreAndGold()
    {
        var state = GameStateCalculator.CreateInitial(100, 10);

        var result = GameStateCalculator.EnemyKilled(state, 15);

        Assert.Equal(10, result.State.Score);
        Assert.Equal(115, result.State.Gold);
    }

    [Fact]
    public void EnemyKilled_AccumulatesAcrossMultipleCalls()
    {
        var state = GameStateCalculator.CreateInitial(100, 10);

        var r1 = GameStateCalculator.EnemyKilled(state, 10);
        var r2 = GameStateCalculator.EnemyKilled(r1.State, 20);

        Assert.Equal(20, r2.State.Score);
        Assert.Equal(130, r2.State.Gold);
    }

    [Fact]
    public void EnemyReachedEnd_ReducesLives()
    {
        var state = GameStateCalculator.CreateInitial(100, 5);

        var result = GameStateCalculator.EnemyReachedEnd(state, 1);

        Assert.Equal(4, result.State.Lives);
        Assert.False(result.IsGameOver);
    }

    [Fact]
    public void EnemyReachedEnd_LivesReachZero_IsGameOver()
    {
        var state = GameStateCalculator.CreateInitial(100, 1);

        var result = GameStateCalculator.EnemyReachedEnd(state, 1);

        Assert.Equal(0, result.State.Lives);
        Assert.True(result.IsGameOver);
        Assert.True(result.State.IsGameOver);
    }

    [Fact]
    public void EnemyReachedEnd_DamageGreaterThanLives_IsGameOver()
    {
        var state = GameStateCalculator.CreateInitial(100, 2);

        var result = GameStateCalculator.EnemyReachedEnd(state, 3);

        Assert.Equal(-1, result.State.Lives);
        Assert.True(result.IsGameOver);
    }

    [Fact]
    public void TryPlaceTower_CanAfford_DeductsGold()
    {
        var state = GameStateCalculator.CreateInitial(200, 10);

        var result = GameStateCalculator.TryPlaceTower(state, 50);

        Assert.True(result.CanAfford);
        Assert.Equal(150, result.State.Gold);
    }

    [Fact]
    public void TryPlaceTower_ExactGold_CanAfford()
    {
        var state = GameStateCalculator.CreateInitial(50, 10);

        var result = GameStateCalculator.TryPlaceTower(state, 50);

        Assert.True(result.CanAfford);
        Assert.Equal(0, result.State.Gold);
    }

    [Fact]
    public void TryPlaceTower_NotEnoughGold_CannotAfford()
    {
        var state = GameStateCalculator.CreateInitial(30, 10);

        var result = GameStateCalculator.TryPlaceTower(state, 50);

        Assert.False(result.CanAfford);
        Assert.Equal(30, result.State.Gold);
    }

    [Fact]
    public void TryPlaceTower_NotEnoughGold_DoesNotMutateState()
    {
        var state = GameStateCalculator.CreateInitial(30, 10);

        var result = GameStateCalculator.TryPlaceTower(state, 50);

        Assert.Equal(state.Gold, result.State.Gold);
        Assert.Equal(state.Lives, result.State.Lives);
        Assert.Equal(state.Score, result.State.Score);
    }

    [Fact]
    public void AdvanceWave_IncrementsWaveNumber()
    {
        var state = GameStateCalculator.CreateInitial(100, 10);

        state = GameStateCalculator.AdvanceWave(state);
        Assert.Equal(1, state.WaveNumber);

        state = GameStateCalculator.AdvanceWave(state);
        Assert.Equal(2, state.WaveNumber);
    }

    [Fact]
    public void AdvanceWave_DoesNotAffectOtherFields()
    {
        var state = GameStateCalculator.CreateInitial(200, 20);

        state = GameStateCalculator.AdvanceWave(state);

        Assert.Equal(200, state.Gold);
        Assert.Equal(20, state.Lives);
        Assert.Equal(0, state.Score);
        Assert.False(state.IsGameOver);
    }
}
