namespace SwarmSurvivor;

public static class TickAccumulator
{
    // Standard 60Hz simulation tick.
    public const float DefaultFixedTimeStep = 1f / 60f;

    // Maximum ticks per frame to prevent spiral-of-death after long pause.
    public const int MaxTicksPerFrame = 5;

    // Given current accumulator and deltaTime, returns:
    //   - newAccumulator (residual time after consuming ticks)
    //   - ticksToProcess (how many fixed-step sim updates to run this frame, capped by MaxTicksPerFrame)
    public static (float NewAccumulator, int TicksToProcess) Advance(
        float accumulator, float dt, float fixedTimeStep, int maxTicks)
    {
        if (fixedTimeStep <= 0f)
        {
            return (0f, 0);
        }

        float acc = accumulator + dt;
        int ticks = (int)(acc / fixedTimeStep);

        if (ticks > maxTicks)
        {
            ticks = maxTicks;
            acc = 0f;
        }
        else
        {
            acc -= ticks * fixedTimeStep;
        }

        return (acc, ticks);
    }

    // Convenience overload using DefaultFixedTimeStep + MaxTicksPerFrame.
    public static (float NewAccumulator, int TicksToProcess) Advance(float accumulator, float dt)
    {
        return Advance(accumulator, dt, DefaultFixedTimeStep, MaxTicksPerFrame);
    }
}
