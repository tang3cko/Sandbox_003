namespace EscapeRoom;

using Godot;

public static class InputGlyphResolver
{
    public static string Resolve(string action)
    {
        if (string.IsNullOrEmpty(action) || !InputMap.HasAction(action))
            return action?.ToUpperInvariant() ?? "";

        var events = InputMap.ActionGetEvents(action);
        foreach (var e in events)
        {
            switch (e)
            {
                case InputEventKey key:
                {
                    var keycode = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
                    var label = OS.GetKeycodeString(keycode);
                    if (!string.IsNullOrEmpty(label)) return label.ToUpperInvariant();
                    break;
                }
                case InputEventMouseButton mouse:
                    return mouse.ButtonIndex switch
                    {
                        MouseButton.Left => "LMB",
                        MouseButton.Right => "RMB",
                        MouseButton.Middle => "MMB",
                        _ => $"M{(int)mouse.ButtonIndex}",
                    };
                case InputEventJoypadButton pad:
                    return $"PAD{(int)pad.ButtonIndex}";
            }
        }

        return action.ToUpperInvariant();
    }
}
