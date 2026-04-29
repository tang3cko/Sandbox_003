namespace SwarmSurvivor;

using System.Collections.Generic;
using Godot;

public partial class PlayerWeaponSystem : Node
{
    [Export] public WeaponConfig StartingWeapon { get; set; }

    public ProjectileManager ProjectileManager { get; set; }
    public SwarmManager SwarmManager { get; set; }

    private NetworkedPlayer _player;
    private readonly List<ActiveWeapon> _weapons = new();

    private struct ActiveWeapon
    {
        public WeaponConfig Config;
        public float Timer;
        public float DamageMultiplier;
        public float SpeedMultiplier;
    }

    public void AddWeapon(WeaponConfig config)
    {
        if (config == null) return;

        _weapons.Add(new ActiveWeapon
        {
            Config = config,
            Timer = 0f,
            DamageMultiplier = 1f,
            SpeedMultiplier = 1f,
        });
    }

    public override void _Ready()
    {
        _player = GetParent<NetworkedPlayer>();

        if (StartingWeapon != null)
        {
            AddWeapon(StartingWeapon);
        }
    }

    public override void _Process(double delta)
    {
        if (Multiplayer.MultiplayerPeer == null) return;
        if (!Multiplayer.IsServer()) return;
        if (_player == null) return;
        if (ProjectileManager == null || SwarmManager == null) return;
        if (_player.IsDead) return;

        float dt = (float)delta;
        float playerX = _player.GlobalPosition.X;
        float playerZ = _player.GlobalPosition.Z;

        for (int i = 0; i < _weapons.Count; i++)
        {
            var weapon = _weapons[i];
            var (newTimer, shouldFire) = PlayerWeaponCalculator.TickFireTimer(
                weapon.Timer, dt, weapon.Config.FireRate, weapon.SpeedMultiplier);
            weapon.Timer = newTimer;

            if (shouldFire)
            {
                FireWeapon(weapon, playerX, playerZ);
            }

            _weapons[i] = weapon;
        }
    }

    private void FireWeapon(ActiveWeapon weapon, float playerX, float playerZ)
    {
        // Phase 2C-2: only Projectile-type weapons. Aura/Orbital out of scope.
        if (weapon.Config.Type != WeaponType.Projectile) return;

        int damage = (int)(weapon.Config.Damage * weapon.DamageMultiplier);

        var nearest = SwarmManager.GetNearestAlivePosition(playerX, playerZ);
        float dx = nearest.x - playerX;
        float dz = nearest.z - playerZ;
        float dist = Mathf.Sqrt(dx * dx + dz * dz);

        if (dist >= weapon.Config.Range * PlayerWeaponCalculator.FireRangeMultiplier) return;

        // Surrounded / overlapping enemy: fire toward player's facing direction
        var basis = _player.GlobalTransform.Basis;
        float facingX = -basis.Z.X;
        float facingZ = -basis.Z.Z;
        var (nx, nz) = PlayerWeaponCalculator.SelectFireDirection(
            dist, dx, dz, facingX, facingZ, PlayerWeaponCalculator.MinFireDirectionDist);

        PeerIdParser.TryParseFromName(_player.Name.ToString(), out var ownerPeerId);

        var (muzzleX, muzzleZ) = PlayerWeaponCalculator.ComputeMuzzlePosition(
            playerX, playerZ, nx, nz, PlayerWeaponCalculator.DefaultMuzzleOffset);

        for (int p = 0; p < weapon.Config.ProjectileCount; p++)
        {
            var (dirX, dirZ) = PlayerWeaponCalculator.ApplyAngleSpread(
                nx, nz, p, weapon.Config.ProjectileCount,
                PlayerWeaponCalculator.DefaultSpreadAngleStep);

            ProjectileManager.SpawnProjectile(
                muzzleX, muzzleZ, dirX, dirZ,
                weapon.Config.ProjectileSpeed,
                damage, weapon.Config.Range,
                0, ownerPeerId);
        }

        var muzzlePos = new Vector3(muzzleX, _player.GlobalPosition.Y + PlayerWeaponCalculator.DefaultMuzzleOffset, muzzleZ);
        Rpc(MethodName.OnWeaponFired, muzzlePos, weapon.Config.ProjectileColor);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,
         CallLocal = true,
         TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
         TransferChannel = NetworkConfig.ChannelPlayer)]
    private void OnWeaponFired(Vector3 worldPos, Color color)
    {
        MuzzleFlashFX.Play(GetTree().CurrentScene, worldPos, color);
    }
}
