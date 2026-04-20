namespace Collector;

using Godot;

public partial class Player : CharacterBody2D
{
    private const int HALF_SIZE = 20;

    private static readonly Color PLAYER_COLOR = new(0.2f, 0.4f, 0.9f);

    private float speed;
    private bool isInvincible;
    private float invincibleTimer;
    private bool isActive = true;

    public bool IsInvincible => isInvincible;

    public void Initialize(float speed)
    {
        this.speed = speed;
    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            Velocity = Vector2.Zero;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!isActive)
            return;

        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = input * speed;
        MoveAndSlide();

        if (isInvincible)
        {
            invincibleTimer -= (float)delta;
            if (invincibleTimer <= 0f)
            {
                isInvincible = false;
                Modulate = Colors.White;
            }
        }
    }

    public void StartInvincibility(float duration)
    {
        isInvincible = true;
        invincibleTimer = duration;
        Modulate = new Color(1f, 1f, 1f, 0.5f);
    }

    public override void _Draw()
    {
        DrawRect(
            new Rect2(-HALF_SIZE, -HALF_SIZE, HALF_SIZE * 2, HALF_SIZE * 2),
            PLAYER_COLOR
        );
    }
}
