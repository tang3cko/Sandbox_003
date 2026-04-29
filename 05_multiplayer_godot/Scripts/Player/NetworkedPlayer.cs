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
    private double _invincibleUntilSec;

    public int OwnerPeerId
    {
        get
        {
            PeerIdParser.TryParseFromName(Name.ToString(), out var id);
            return id;
        }
    }
    public bool IsLocalAuthority =>
        Multiplayer.MultiplayerPeer != null
        && GetMultiplayerAuthority() == Multiplayer.GetUniqueId();

    public void TakeDamage(int damage)
    {
        if (Multiplayer.MultiplayerPeer == null) return;
        if (!Multiplayer.IsServer()) return;
        if (IsDead) return;

        var now = Time.GetTicksMsec() / 1000.0;
        if (PlayerCombatCalculator.IsInvincible(now, _invincibleUntilSec)) return;

        CurrentHealth -= damage;
        if (CurrentHealth < 0) CurrentHealth = 0;
        _invincibleUntilSec = PlayerCombatCalculator.NextInvincibleUntil(now);

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
        var selfId = Multiplayer.MultiplayerPeer != null ? Multiplayer.GetUniqueId() : 0;
        NetLog.Info($"[NetworkedPlayer] Ready: peer={Name} role={role} pos={GlobalPosition} authority={GetMultiplayerAuthority()} self={selfId}");
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
        var (dirX, dirZ, hasInput) = InputDirectionCalculator.NormalizeWithDeadzone(input.X, input.Y);
        var (velX, velZ) = InputDirectionCalculator.ComputeVelocity(dirX, dirZ, MoveSpeed, hasInput);
        Velocity = new Vector3(velX, 0f, velZ);

        if (hasInput)
        {
            var (tx, tz) = InputDirectionCalculator.ComputeLookTarget(
                GlobalPosition.X, GlobalPosition.Z, dirX, dirZ);
            LookAt(new Vector3(tx, GlobalPosition.Y, tz), Vector3.Up);
        }

        MoveAndSlide();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
         CallLocal = true,
         TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
         TransferChannel = NetworkConfig.ChannelPlayer)]
    private void OnHitEffect()
    {
        NetLog.Info($"[NetworkedPlayer] HitEffect: peer={Name} HP={CurrentHealth}");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,
         CallLocal = true,
         TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
         TransferChannel = NetworkConfig.ChannelPlayer)]
    private void OnDeathEffect()
    {
        NetLog.Info($"[NetworkedPlayer] DeathEffect: peer={Name}");
        ApplyDeadVisuals();
    }

    private void ApplyDeadVisuals()
    {
        if (_meshInstance == null) return;

        var grayColor = new Color(0.3f, 0.3f, 0.3f);
        var material = new StandardMaterial3D
        {
            AlbedoColor = grayColor,
            Emission = grayColor * 0.1f,
            EmissionEnabled = true,
        };
        _meshInstance.MaterialOverride = material;

        RotationDegrees = new Vector3(90f, RotationDegrees.Y, 0f);
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
