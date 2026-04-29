namespace SwarmSurvivor;

public static class ScreenShakeCalculator
{
    // Returns intensity at the given remaining time, given initial intensity and duration.
    // Linear decay: at remaining=duration → full intensity, at remaining=0 → 0.
    public static float CalculateIntensity(float initialIntensity, float remainingTime, float duration)
    {
        if (duration <= 0f) return 0f;
        return initialIntensity * (remainingTime / duration);
    }
}
