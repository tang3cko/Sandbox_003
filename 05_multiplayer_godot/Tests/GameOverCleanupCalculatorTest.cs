using Xunit;

namespace SwarmSurvivor.Tests;

public class GameOverCleanupCalculatorTest
{
    [Fact]
    public void Compute_NotGameOver_ReturnsEmptyPlan()
    {
        var plan = GameOverCleanupCalculator.Compute(
            liveEnemyCount: 50,
            liveProjectileCount: 30,
            queuedWaveCount: 10,
            isGameOver: false);

        Assert.False(plan.ClearEnemies);
        Assert.False(plan.ClearProjectiles);
        Assert.False(plan.ClearQueuedSpawns);
        Assert.Equal(0, plan.EnemiesToClear);
        Assert.Equal(0, plan.ProjectilesToClear);
        Assert.Equal(0, plan.QueuedSpawnsToClear);
        Assert.Equal(GameOverCleanupCalculator.Empty, plan);
    }

    [Fact]
    public void Compute_GameOver_AllCountsPositive_PlanClearsAll()
    {
        var plan = GameOverCleanupCalculator.Compute(
            liveEnemyCount: 42,
            liveProjectileCount: 17,
            queuedWaveCount: 5,
            isGameOver: true);

        Assert.True(plan.ClearEnemies);
        Assert.True(plan.ClearProjectiles);
        Assert.True(plan.ClearQueuedSpawns);
        Assert.Equal(42, plan.EnemiesToClear);
        Assert.Equal(17, plan.ProjectilesToClear);
        Assert.Equal(5, plan.QueuedSpawnsToClear);
    }

    [Fact]
    public void Compute_GameOver_AllCountsZero_PlanClearsNothing_Idempotent()
    {
        var plan = GameOverCleanupCalculator.Compute(
            liveEnemyCount: 0,
            liveProjectileCount: 0,
            queuedWaveCount: 0,
            isGameOver: true);

        Assert.False(plan.ClearEnemies);
        Assert.False(plan.ClearProjectiles);
        Assert.False(plan.ClearQueuedSpawns);
        Assert.Equal(0, plan.EnemiesToClear);
        Assert.Equal(0, plan.ProjectilesToClear);
        Assert.Equal(0, plan.QueuedSpawnsToClear);
    }

    [Fact]
    public void Compute_GameOver_PartialCounts_OnlyFlagsNonZero()
    {
        var plan = GameOverCleanupCalculator.Compute(
            liveEnemyCount: 3,
            liveProjectileCount: 0,
            queuedWaveCount: 1,
            isGameOver: true);

        Assert.True(plan.ClearEnemies);
        Assert.False(plan.ClearProjectiles);
        Assert.True(plan.ClearQueuedSpawns);
        Assert.Equal(3, plan.EnemiesToClear);
        Assert.Equal(0, plan.ProjectilesToClear);
        Assert.Equal(1, plan.QueuedSpawnsToClear);
    }

    [Fact]
    public void Compute_GameOver_IntMaxValueCounts_NoOverflow()
    {
        var plan = GameOverCleanupCalculator.Compute(
            liveEnemyCount: int.MaxValue,
            liveProjectileCount: int.MaxValue,
            queuedWaveCount: int.MaxValue,
            isGameOver: true);

        Assert.True(plan.ClearEnemies);
        Assert.True(plan.ClearProjectiles);
        Assert.True(plan.ClearQueuedSpawns);
        Assert.Equal(int.MaxValue, plan.EnemiesToClear);
        Assert.Equal(int.MaxValue, plan.ProjectilesToClear);
        Assert.Equal(int.MaxValue, plan.QueuedSpawnsToClear);
    }

    [Fact]
    public void Compute_GameOver_NegativeCountsTreatedAsZero()
    {
        var plan = GameOverCleanupCalculator.Compute(
            liveEnemyCount: -5,
            liveProjectileCount: -1,
            queuedWaveCount: -100,
            isGameOver: true);

        Assert.False(plan.ClearEnemies);
        Assert.False(plan.ClearProjectiles);
        Assert.False(plan.ClearQueuedSpawns);
        Assert.Equal(0, plan.EnemiesToClear);
        Assert.Equal(0, plan.ProjectilesToClear);
        Assert.Equal(0, plan.QueuedSpawnsToClear);
    }

    [Fact]
    public void Compute_Deterministic_SameInputProducesSameOutput()
    {
        var p1 = GameOverCleanupCalculator.Compute(11, 7, 2, true);
        var p2 = GameOverCleanupCalculator.Compute(11, 7, 2, true);
        var p3 = GameOverCleanupCalculator.Compute(11, 7, 2, true);

        Assert.Equal(p1, p2);
        Assert.Equal(p2, p3);
    }
}
