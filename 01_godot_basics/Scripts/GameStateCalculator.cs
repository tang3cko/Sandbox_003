namespace Collector;

public readonly record struct CoinCollectedResult(
    GameStateData State,
    bool IsWaveCleared
);

public readonly record struct HazardHitResult(
    GameStateData State,
    bool IsGameOver
);

public static class GameStateCalculator
{
    public static GameStateData CreateInitial(int startingLives)
    {
        return new GameStateData
        {
            score = 0,
            lives = startingLives,
            wave = 0,
            coinsRemaining = 0,
            isGameOver = false,
        };
    }

    public static GameStateData StartWave(GameStateData state, int coinsPerWave)
    {
        state.wave++;
        state.coinsRemaining = coinsPerWave;
        return state;
    }

    public static CoinCollectedResult CollectCoin(GameStateData state)
    {
        state.score++;
        state.coinsRemaining--;
        bool isWaveCleared = state.coinsRemaining <= 0;
        return new CoinCollectedResult(state, isWaveCleared);
    }

    public static HazardHitResult HitByHazard(GameStateData state)
    {
        state.lives--;
        bool isGameOver = state.lives <= 0;
        if (isGameOver)
        {
            state.isGameOver = true;
        }
        return new HazardHitResult(state, isGameOver);
    }
}
