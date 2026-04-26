namespace SwarmSurvivor;

using Godot;

public partial class NetworkedPlayer : CharacterBody3D, IDamageable
{
    [Export] public float MoveSpeed { get; set; } = 6f;
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int CurrentHealth { get; set; }
    [Export] public bool IsDead { get; set; }

    [Signal] public delegate void PlayerDiedEventHandler(long peerId);

    private MeshInstance3D _meshInstance;

    public int OwnerPeerId => int.Parse(Name);
    public bool IsLocalAuthority =>
        Multiplayer.MultiplayerPeer != null
        && GetMultiplayerAuthority() == Multiplayer.GetUniqueId();

    public void TakeDamage(int damage)
    {
        if (Multiplayer.MultiplayerPeer == null) return;
        if (!Multiplayer.IsServer()) return;
        if (IsDead) return;

        CurrentHealth -= damage;
        if (CurrentHealth < 0) CurrentHealth = 0;

        Rpc(MethodName.OnHitEffect);

        if (CurrentHealth <= 0)
        {
            IsDead = true;
            Rpc(MethodName.OnDeathEffect);
            EmitSignal(SignalName.PlayerDied, OwnerPeerId);
        }
    }

    public override void _Ready()
    {
        _meshInstance = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
        ApplyAuthorityVisuals();

        if (Multiplayer.MultiplayerPeer != null && Multiplayer.IsServer())
        {
            CurrentHealth = MaxHealth;
        }

        var role = IsLocalAuthority ? "LOCAL" : "REMOTE";
        GD.Print($"[NetworkedPlayer] Ready: peer={Name} role={role} pos={GlobalPosition} authority={GetMultiplayerAuthority()} self={Multiplayer.GetUniqueId()}");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsLocalAuthority) return;

        if (IsDead)
        {
            Velocity = Vector3.Zero;
            MoveAndSlide();
            return;
        }

        var input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        var direction = new Vector3(input.X, 0f, input.Y);

        if (direction.LengthSquared() > 0.01f)
        {
            direction = direction.Normalized();
            Velocity = direction * MoveSpeed;

            var lookTarget = GlobalPosition + direction;
            LookAt(lookTarget, Vector3.Up);
        }
        else
        {
            Velocity = Vector3.Zero;
        }

        MoveAndSlide();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
         CallLocal = true,
         TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void OnHitEffect()
    {
        GD.Print($"[NetworkedPlayer] HitEffect: peer={Name} HP={CurrentHealth}");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
         CallLocal = true,
         TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void OnDeathEffect()
    {
        GD.Print($"[NetworkedPlayer] DeathEffect: peer={Name}");
    }

    private void ApplyAuthorityVisuals()
    {
        if (_meshInstance == null) return;

        var color = IsLocalAuthority
            ? new Color(0.2f, 0.6f, 1f)
            : new Color(1f, 0.4f, 0.3f);

        var material = new StandardMaterial3D
        {
            AlbedoColor = color,
            Emission = color * 0.4f,
            EmissionEnabled = true,
        };
        _meshInstance.MaterialOverride = material;
    }
}
