namespace SwarmSurvivor;

using System.Diagnostics;
using Godot;

public static class NetLog
{
    [Conditional("DEBUG")]
    public static void Info(string message)
    {
        GD.Print(message);
    }

    [Conditional("DEBUG")]
    public static void Warn(string message)
    {
        GD.PushWarning(message);
        GD.Print($"[WARN] {message}");
    }

    public static void Error(string message)
    {
        // Errors always log even in release — they signal real problems.
        GD.PushError(message);
    }
}
