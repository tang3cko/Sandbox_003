namespace EscapeRoom;

using Godot;

public interface IInteractor
{
    Node3D Body { get; }
    Vector3 GlobalPosition { get; }
}
