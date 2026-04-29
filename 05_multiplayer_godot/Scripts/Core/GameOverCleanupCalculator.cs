namespace SwarmSurvivor;

// Pure calculator: decides what cleanup actions to perform when game-over fires.
// Zero Godot dependencies — Plan is data only; managers execute the actions.
public readonly record struct CleanupPlan(
    bool ClearEnemies,
    bool ClearProjectiles,
    bool ClearQueuedSpawns,
    int EnemiesToClear,
    int ProjectilesToClear,
    int QueuedSpawnsToClear);

public static class GameOverCleanupCalculator
{
    public static CleanupPlan Empty => new(
        ClearEnemies: false,
        ClearProjectiles: false,
        ClearQueuedSpawns: false,
        EnemiesToClear: 0,
        ProjectilesToClear: 0,
        QueuedSpawnsToClear: 0);

    // Returns a deterministic cleanup plan. Pure: same input → same output, no side effects.
    // - isGameOver=false → empty plan (no action).
    // - isGameOver=true → flag is set per category whenever its count > 0; counts are clamped to >= 0.
    //   Counts of 0 yield ClearX=false (idempotent).
    public static CleanupPlan Compute(
        int liveEnemyCount,
        int liveProjectileCount,
        int queuedWaveCount,
        bool isGameOver)
    {
        if (!isGameOver) return Empty;

        int enemies = liveEnemyCount > 0 ? liveEnemyCount : 0;
        int projectiles = liveProjectileCount > 0 ? liveProjectileCount : 0;
        int queued = queuedWaveCount > 0 ? queuedWaveCount : 0;

        return new CleanupPlan(
            ClearEnemies: enemies > 0,
            ClearProjectiles: projectiles > 0,
            ClearQueuedSpawns: queued > 0,
            EnemiesToClear: enemies,
            ProjectilesToClear: projectiles,
            QueuedSpawnsToClear: queued);
    }
}
