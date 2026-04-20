namespace TowerDefense;

public readonly record struct EnemyKilledResult(
    GameState State
);

public readonly record struct EnemyReachedEndResult(
    GameState State,
    bool IsGameOver
);

public readonly record struct TowerPlacedResult(
    GameState State,
    bool CanAfford
);

public struct GameState
{
    public int Gold;
    public int Lives;
    public int Score;
    public int WaveNumber;
    public bool IsGameOver;
}

public static class GameStateCalculator
{
    public static GameState CreateInitial(int startingGold, int startingLives)
    {
        return new GameState
        {
            Gold = startingGold,
            Lives = startingLives,
            Score = 0,
            WaveNumber = 0,
            IsGameOver = false,
        };
    }

    public static EnemyKilledResult EnemyKilled(GameState state, int goldReward)
    {
        state.Score += 10;
        state.Gold += goldReward;
        return new EnemyKilledResult(state);
    }

    public static EnemyReachedEndResult EnemyReachedEnd(GameState state, int damage)
    {
        state.Lives -= damage;
        bool isGameOver = state.Lives <= 0;
        if (isGameOver)
            state.IsGameOver = true;
        return new EnemyReachedEndResult(state, isGameOver);
    }

    public static TowerPlacedResult TryPlaceTower(GameState state, int cost)
    {
        if (state.Gold < cost)
            return new TowerPlacedResult(state, false);

        state.Gold -= cost;
        return new TowerPlacedResult(state, true);
    }

    public static GameState AdvanceWave(GameState state)
    {
        state.WaveNumber++;
        return state;
    }
}
