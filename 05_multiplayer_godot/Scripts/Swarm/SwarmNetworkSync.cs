namespace SwarmSurvivor;

using System;
using Godot;

public partial class SwarmNetworkSync : Node
{
    private const int MaxEntities = 600;

    [Export] public WaveConfig WaveConfig { get; set; }

    private bool _isServer;
    private float _accumulator;
    private byte[] _sendBuffer = new byte[SwarmSnapshot.CalculateBufferSize(MaxEntities)];

    private SwarmManager _swarmManager;
    private SwarmRenderer _clientRenderer;

    private readonly float[] _posX = new float[MaxEntities];
    private readonly float[] _posZ = new float[MaxEntities];
    private readonly float[] _velX = new float[MaxEntities];
    private readonly float[] _velZ = new float[MaxEntities];
    private readonly int[] _typeIndex = new int[MaxEntities];
    private readonly float[] _flashTimer = new float[MaxEntities];
    private readonly float[] _deathTimer = new float[MaxEntities];
    private int _activeCount;
    private int _snapshotsReceived;

    private readonly float[] _renderPosX = new float[MaxEntities];
    private readonly float[] _renderPosZ = new float[MaxEntities];
    private readonly float[] _startPosX = new float[MaxEntities];
    private readonly float[] _startPosZ = new float[MaxEntities];
    private float _interpolationElapsed;
    private int _previousActiveCount;

    public int ActiveCount => _activeCount;
    public int SnapshotsReceived => _snapshotsReceived;

    public override void _Ready()
    {
        _isServer = Multiplayer.MultiplayerPeer != null && Multiplayer.IsServer();
        if (_isServer)
        {
            _swarmManager = GetParent().GetNodeOrNull<SwarmManager>("SwarmManager");
            if (_swarmManager == null)
            {
                GD.PrintErr("[SwarmNetworkSync] Server-side SwarmManager not found at expected path");
            }
            else
            {
                GD.Print("[SwarmNetworkSync] Server broadcaster ready");
            }
        }
        else
        {
            _clientRenderer = GetNodeOrNull<SwarmRenderer>("SwarmRenderer");
            if (_clientRenderer == null)
            {
                GD.PrintErr("[SwarmNetworkSync] Client-side SwarmRenderer child missing");
            }
            else
            {
                GD.Print("[SwarmNetworkSync] Client receiver ready");
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_isServer)
        {
            if (_swarmManager == null) return;
            _accumulator += (float)delta;
            if (_accumulator < NetworkConfig.SnapshotInterval) return;
            _accumulator -= NetworkConfig.SnapshotInterval;
            BroadcastSnapshot();
        }
        else
        {
            if (_clientRenderer == null || WaveConfig?.EnemyTypes == null) return;

            _interpolationElapsed += (float)delta;
            float t = NetworkConfig.SnapshotInterval > 0f
                ? Mathf.Clamp(_interpolationElapsed / NetworkConfig.SnapshotInterval, 0f, 1f)
                : 1f;
            for (int i = 0; i < _activeCount; i++)
            {
                _renderPosX[i] = _startPosX[i] + (_posX[i] - _startPosX[i]) * t;
                _renderPosZ[i] = _startPosZ[i] + (_posZ[i] - _startPosZ[i]) * t;
            }

            _clientRenderer.UpdateInstances(
                _renderPosX, _renderPosZ, _velX, _velZ,
                _typeIndex, _flashTimer, _deathTimer,
                _activeCount, WaveConfig.EnemyTypes);
        }
    }

    private void BroadcastSnapshot()
    {
        _swarmManager.GetSoAReference(
            out var posX, out var posZ, out var velX, out var velZ,
            out var typeIndex, out var flashTimer, out var deathTimer);

        int count = _swarmManager.ActiveCount;
        int needed = SwarmSnapshot.CalculateBufferSize(count);
        if (_sendBuffer.Length < needed) _sendBuffer = new byte[needed];

        SwarmSnapshot.Encode(_sendBuffer, posX, posZ, velX, velZ,
            typeIndex, flashTimer, deathTimer, count);

        var packet = new byte[needed];
        Array.Copy(_sendBuffer, packet, needed);
        Rpc(MethodName.ApplySnapshot, packet);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,
         CallLocal = false,
         TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered,
         TransferChannel = NetworkConfig.ChannelSwarm)]
    private void ApplySnapshot(byte[] data)
    {
        int newCount = SwarmSnapshot.Decode(data, data.Length,
            _posX, _posZ, _velX, _velZ,
            _typeIndex, _flashTimer, _deathTimer);

        for (int i = 0; i < newCount; i++)
        {
            if (i < _previousActiveCount)
            {
                _startPosX[i] = _renderPosX[i];
                _startPosZ[i] = _renderPosZ[i];
            }
            else
            {
                _startPosX[i] = _posX[i];
                _startPosZ[i] = _posZ[i];
                _renderPosX[i] = _posX[i];
                _renderPosZ[i] = _posZ[i];
            }
        }

        _previousActiveCount = newCount;
        _activeCount = newCount;
        _interpolationElapsed = 0f;
        _snapshotsReceived++;
    }
}
