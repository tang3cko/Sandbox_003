namespace SwarmSurvivor;

using System;
using Godot;

public partial class SwarmNetworkSync : Node
{
    private const int MaxEntities = 600;

    [Export] public WaveConfig WaveConfig { get; set; }

    private float _accumulator;

    private bool IsServer => Multiplayer.MultiplayerPeer != null && Multiplayer.IsServer();

    private SwarmManager _swarmManager;
    private SwarmRenderer _clientRenderer;

    private readonly float[] _posX = new float[MaxEntities];
    private readonly float[] _posZ = new float[MaxEntities];
    private readonly float[] _velX = new float[MaxEntities];
    private readonly float[] _velZ = new float[MaxEntities];
    private readonly int[] _typeIndex = new int[MaxEntities];
    private readonly float[] _flashTimer = new float[MaxEntities];
    private readonly float[] _deathTimer = new float[MaxEntities];
    private readonly int[] _entityId = new int[MaxEntities];
    private readonly int[] _previousEntityId = new int[MaxEntities];
    private int _activeCount;
    private int _snapshotsReceived;

    private readonly float[] _renderPosX = new float[MaxEntities];
    private readonly float[] _renderPosZ = new float[MaxEntities];
    private readonly float[] _startPosX = new float[MaxEntities];
    private readonly float[] _startPosZ = new float[MaxEntities];
    private readonly float[] _oldRenderPosX = new float[MaxEntities];
    private readonly float[] _oldRenderPosZ = new float[MaxEntities];
    private readonly int[] _oldEntityId = new int[MaxEntities];
    private float _interpolationElapsed;
    private int _previousActiveCount;

    public int ActiveCount => _activeCount;
    public int SnapshotsReceived => _snapshotsReceived;

    public override void _Ready()
    {
        if (IsServer)
        {
            _swarmManager = GetParent().GetNodeOrNull<SwarmManager>("SwarmManager");
            if (_swarmManager == null)
            {
                NetLog.Error("[SwarmNetworkSync] Server-side SwarmManager not found at expected path");
            }
            else
            {
                NetLog.Info("[SwarmNetworkSync] Server broadcaster ready");
            }
        }
        else
        {
            _clientRenderer = GetNodeOrNull<SwarmRenderer>("SwarmRenderer");
            if (_clientRenderer == null)
            {
                NetLog.Error("[SwarmNetworkSync] Client-side SwarmRenderer child missing");
            }
            else
            {
                NetLog.Info("[SwarmNetworkSync] Client receiver ready");
            }
        }
    }

    public override void _Process(double delta)
    {
        if (IsServer)
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
            float t = InterpolationStartCalculator.ComputeAlpha(
                _interpolationElapsed, NetworkConfig.SnapshotInterval);
            for (int i = 0; i < _activeCount; i++)
            {
                _renderPosX[i] = InterpolationStartCalculator.Lerp(_startPosX[i], _posX[i], t);
                _renderPosZ[i] = InterpolationStartCalculator.Lerp(_startPosZ[i], _posZ[i], t);
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
            out var typeIndex, out var flashTimer, out var deathTimer, out var entityId);

        int count = _swarmManager.ActiveCount;
        int needed = SwarmSnapshot.CalculateBufferSize(count);
        var packet = new byte[needed];
        SwarmSnapshot.Encode(packet, posX, posZ, velX, velZ,
            typeIndex, flashTimer, deathTimer, entityId, count);
        Rpc(MethodName.ApplySnapshot, packet);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,
         CallLocal = false,
         TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered,
         TransferChannel = NetworkConfig.ChannelSwarm)]
    private void ApplySnapshot(byte[] data)
    {
        Array.Copy(_renderPosX, _oldRenderPosX, _previousActiveCount);
        Array.Copy(_renderPosZ, _oldRenderPosZ, _previousActiveCount);
        Array.Copy(_previousEntityId, _oldEntityId, _previousActiveCount);
        int oldCount = _previousActiveCount;

        int newCount = SwarmSnapshot.Decode(data, data.Length,
            _posX, _posZ, _velX, _velZ,
            _typeIndex, _flashTimer, _deathTimer, _entityId);

        for (int i = 0; i < newCount; i++)
        {
            int id = _entityId[i];
            int oldIdx = -1;
            for (int j = 0; j < oldCount; j++)
            {
                if (_oldEntityId[j] == id) { oldIdx = j; break; }
            }
            if (oldIdx >= 0)
            {
                _startPosX[i] = _oldRenderPosX[oldIdx];
                _startPosZ[i] = _oldRenderPosZ[oldIdx];
            }
            else
            {
                _startPosX[i] = _posX[i];
                _startPosZ[i] = _posZ[i];
                _renderPosX[i] = _posX[i];
                _renderPosZ[i] = _posZ[i];
            }
        }

        Array.Copy(_entityId, _previousEntityId, newCount);
        _previousActiveCount = newCount;
        _activeCount = newCount;
        _interpolationElapsed = 0f;
        _snapshotsReceived++;
    }
}
