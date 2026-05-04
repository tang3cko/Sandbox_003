using Godot;
using VoxelTerrain.World;

namespace VoxelTerrain.Player;

public partial class DiggingTool : Node
{
    [Export] public NodePath CameraPath { get; set; }
    [Export] public NodePath WorldPath { get; set; }
    [Export] public float Reach { get; set; } = 40f;
    [Export] public float DigRadius { get; set; } = 2.0f;
    [Export] public float PlaceRadius { get; set; } = 1.5f;

    private Camera3D _camera;
    private VoxelWorld _world;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>(CameraPath);
        _world = GetNode<VoxelWorld>(WorldPath);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("dig"))
        {
            if (TryRaycast(out var hit, surfaceOffset: -0.05f))
                _world.DigSphere(hit, DigRadius);
        }
        else if (@event.IsActionPressed("place"))
        {
            if (TryRaycast(out var hit, surfaceOffset: +0.05f))
                _world.PlaceSphere(hit, PlaceRadius);
        }
    }

    private bool TryRaycast(out Vector3 hitPoint, float surfaceOffset)
    {
        hitPoint = default;
        var space = _camera.GetWorld3D().DirectSpaceState;
        var from = _camera.GlobalPosition;
        var to = from + (-_camera.GlobalTransform.Basis.Z) * Reach;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        var result = space.IntersectRay(query);
        if (result.Count == 0) return false;
        var pos = (Vector3)result["position"];
        var normal = (Vector3)result["normal"];
        hitPoint = pos + normal * surfaceOffset;
        return true;
    }
}
