namespace ArenaSurvivor;

using Godot;
using ReactiveSO;

public partial class PlayerController : CharacterBody3D
{
    [Export] public PlayerConfig Config { get; set; }
    [Export] private IntVariable _health;
    [Export] private FloatVariable _stamina;
    [Export] private VoidEventChannel _onPlayerDied;
    [Export] private IntEventChannel _onPlayerDamaged;
    [Export] private VoidEventChannel _onPlayerDodged;

    private PlayerState _state;
    private Vector3 _dodgeDirection;
    private float _dodgeTimer;
    private float _dodgeCooldownTimer;
    private float _attackTimer;
    private Camera3D _camera;

    public bool IsDodging => _state.IsDodging;
    public bool IsAttacking => _state.IsAttacking;

    public void SetCamera(Camera3D camera)
    {
        _camera = camera;
    }

    public override void _Ready()
    {
        _state = CombatCalculator.CreateInitial(Config.MaxHealth, Config.MaxStamina);
        SyncVariables();
    }

    public override void _PhysicsProcess(double delta)
    {
        var dt = (float)delta;

        // Stamina regen
        var regenResult = CombatCalculator.RegenStamina(_state, Config.StaminaRegenRate * dt);
        _state = regenResult.State;
        if (_stamina != null) _stamina.Value = _state.Stamina;

        // Combo timer
        _state = CombatCalculator.UpdateComboTimer(_state, dt);

        // Dodge timer
        if (_state.IsDodging)
        {
            _dodgeTimer -= dt;
            if (_dodgeTimer <= 0f)
            {
                _state = CombatCalculator.EndDodge(_state);
            }
        }

        // Attack timer
        if (_state.IsAttacking)
        {
            _attackTimer -= dt;
            if (_attackTimer <= 0f)
            {
                _state = CombatCalculator.EndAttack(_state);
            }
        }

        // Dodge cooldown
        if (_dodgeCooldownTimer > 0f)
            _dodgeCooldownTimer -= dt;

        // Movement
        var velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y -= Config.Gravity * dt;

        if (_state.IsDodging)
        {
            velocity.X = _dodgeDirection.X * Config.DodgeSpeed;
            velocity.Z = _dodgeDirection.Z * Config.DodgeSpeed;
        }
        else if (!_state.IsAttacking)
        {
            var input = GetMovementInput();
            velocity.X = input.X * Config.MoveSpeed;
            velocity.Z = input.Z * Config.MoveSpeed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Config.MoveSpeed * dt * 5f);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, Config.MoveSpeed * dt * 5f);
        }

        // Face toward mouse cursor
        RotateTowardMouse(dt);

        Velocity = velocity;
        MoveAndSlide();
    }

    public void HandleDodgeInput()
    {
        if (_dodgeCooldownTimer > 0f) return;

        var dodgeResult = CombatCalculator.TryDodge(_state, Config.DodgeStaminaCost);
        if (!dodgeResult.CanDodge) return;

        _state = dodgeResult.State;
        if (_stamina != null) _stamina.Value = _state.Stamina;

        var input = GetMovementInput();
        _dodgeDirection = input.LengthSquared() > 0.01f ? input.Normalized() : -GlobalTransform.Basis.Z;
        _dodgeTimer = Config.DodgeDuration;
        _dodgeCooldownTimer = Config.DodgeDuration + Config.DodgeCooldown;

        _onPlayerDodged?.Raise();
    }

    public AttackResult HandleAttackInput(int baseDamage)
    {
        var result = CombatCalculator.TryAttack(_state, baseDamage, Config.ComboWindow);
        if (result.Damage > 0)
        {
            _state = result.State;
            _attackTimer = Config.AttackDuration;
        }
        return result;
    }

    public void TakeDamage(int damage)
    {
        var result = CombatCalculator.TakeDamage(_state, damage);
        _state = result.State;
        if (_health != null) _health.Value = _state.Health;
        _onPlayerDamaged?.Raise(damage);

        if (result.IsDead)
        {
            _onPlayerDied?.Raise();
        }
    }

    private Vector3 GetMovementInput()
    {
        var input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        if (input.LengthSquared() < 0.01f) return Vector3.Zero;
        return new Vector3(input.X, 0, input.Y).Normalized();
    }

    private void RotateTowardMouse(float dt)
    {
        if (_camera == null) return;

        var mousePos = GetViewport().GetMousePosition();
        var from = _camera.ProjectRayOrigin(mousePos);
        var dir = _camera.ProjectRayNormal(mousePos);

        if (Mathf.Abs(dir.Y) < 0.001f) return;
        var t = -from.Y / dir.Y;
        if (t < 0) return;

        var worldPos = from + dir * t;
        var toMouse = worldPos - GlobalPosition;
        toMouse.Y = 0;

        if (toMouse.LengthSquared() < 0.1f) return;

        var targetAngle = Mathf.Atan2(-toMouse.X, -toMouse.Z);
        Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, targetAngle, Config.RotationSpeed * dt), 0);
    }

    private void SyncVariables()
    {
        _health?.SetWithoutNotify(_state.Health);
        _stamina?.SetWithoutNotify(_state.Stamina);
        if (_health != null) _health.Value = _state.Health;
        if (_stamina != null) _stamina.Value = _state.Stamina;
    }
}
