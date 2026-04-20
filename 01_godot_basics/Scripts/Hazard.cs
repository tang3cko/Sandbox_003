namespace Collector;

using Godot;

public partial class Hazard : Area2D
{
    private const int HALF_SIZE = 14;

    private static readonly Color HAZARD_COLOR = new(0.9f, 0.2f, 0.2f);

    private Vector2 direction;
    private float speed;
    private Rect2 expandedBounds;

    [Export] public VoidEventChannel OnHitPlayer { get; private set; }

    public void Initialize(float speed, Vector2 direction, Rect2 arenaBounds)
    {
        this.speed = speed;
        this.direction = direction;
        expandedBounds = arenaBounds.Grow(100);
    }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += direction * speed * (float)delta;

        if (!expandedBounds.HasPoint(Position))
        {
            QueueFree();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player && !player.IsInvincible)
        {
            OnHitPlayer?.Raise();
            QueueFree();
        }
    }

    public override void _Draw()
    {
        DrawRect(
            new Rect2(-HALF_SIZE, -HALF_SIZE, HALF_SIZE * 2, HALF_SIZE * 2),
            HAZARD_COLOR
        );
    }
}
