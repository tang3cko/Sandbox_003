namespace Collector;

using Godot;

public partial class Coin : Area2D
{
    private const int HALF_SIZE = 12;

    private static readonly Color COIN_COLOR = new(0.95f, 0.85f, 0.2f);

    [Export] public VoidEventChannel OnCollected { get; private set; }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player)
        {
            OnCollected?.Raise();
            QueueFree();
        }
    }

    public override void _Draw()
    {
        DrawRect(
            new Rect2(-HALF_SIZE, -HALF_SIZE, HALF_SIZE * 2, HALF_SIZE * 2),
            COIN_COLOR
        );
    }
}
