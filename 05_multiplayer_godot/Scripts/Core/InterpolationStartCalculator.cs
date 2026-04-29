namespace SwarmSurvivor;

public static class InterpolationStartCalculator
{
    // Computes the interpolation alpha (0..1) for lerping between the previous
    // snapshot's render position and the latest snapshot's authoritative position.
    //
    // elapsed is time accumulated since the latest snapshot was applied.
    // interval is the expected snapshot interval (i.e. the interpolation duration).
    //
    // Defensive cases:
    //   - interval <= 0: cannot interpolate over zero/negative window, snap to 1 (latest pos).
    //   - elapsed < 0:   treated as 0 (cannot be before the snapshot was received).
    //   - elapsed > interval: clamped to 1 (interpolation finished, no extrapolation).
    //   - NaN inputs:    return 1f (snap to latest, safest fallback).
    public static float ComputeAlpha(float elapsed, float interval)
    {
        if (float.IsNaN(elapsed) || float.IsNaN(interval)) return 1f;
        if (interval <= 0f) return 1f;

        float t = elapsed / interval;
        if (t < 0f) return 0f;
        if (t > 1f) return 1f;
        return t;
    }

    // Linearly interpolates between start and end using the supplied alpha.
    // Pure scalar lerp with no clamping (caller is expected to pass a clamped alpha
    // from ComputeAlpha when 0..1 behavior is required).
    public static float Lerp(float start, float end, float alpha)
    {
        return start + (end - start) * alpha;
    }
}
