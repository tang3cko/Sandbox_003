namespace EscapeRoom;

using Godot;

[GlobalClass]
public partial class TriggerVolume : Area3D
{
    [Signal] public delegate void PlayerEnteredEventHandler();
    [Signal] public delegate void PlayerExitedEventHandler();

    [Export] public string PlayerGroup { get; set; } = "player";
    [Export] public bool OneShot { get; set; } = false;

    private bool _fired;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (!body.IsInGroup(PlayerGroup)) return;
        if (OneShot && _fired) return;
        _fired = true;
        EmitSignal(SignalName.PlayerEntered);
    }

    private void OnBodyExited(Node3D body)
    {
        if (!body.IsInGroup(PlayerGroup)) return;
        EmitSignal(SignalName.PlayerExited);
    }
}
