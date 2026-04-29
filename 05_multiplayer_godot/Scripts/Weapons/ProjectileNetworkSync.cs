namespace SwarmSurvivor;

using System;
using System.Collections.Generic;
using Godot;

public partial class ProjectileNetworkSync : Node
{
    private const int MaxEntities = 200;

    private float _accumulator;

    private bool IsServer => Multiplayer.MultiplayerPeer != null && Multiplayer.IsServer();
    private readonly Dictionary<int, int> _idToOldIndex = new(MaxEntities);

    private ProjectileManager _projectileManager;
    private ProjectileRenderer _clientRenderer;

    private readonly float[] _posX = new float[MaxEntities];
    private readonly float[] _posZ = new float[MaxEntities];
    private readonly float[] _dirX = new float[MaxEntities];
    private readonly float[] _dirZ = new float[MaxEntities];
    private readonly float[] _lifetime = new float[MaxEntities];
    private readonly int[] _ownerId = new int[MaxEntities];
    private readonly int[] _colorIdx = new int[MaxEntities];
    private readonly int[] _projectileId = new int[MaxEntities];
    private readonly int[] _previousProjectileId = new int[MaxEntities];
    private int _activeCount;
    private int _snapshotsReceived;

    private readonly float[] _renderPosX = new float[MaxEntities];
    private readonly float[] _renderPosZ = new float[MaxEntities];
    private readonly float[] _startPosX = new float[MaxEntities];
    private readonly float[] _startPosZ = new float[MaxEntities];
    private readonly float[] _oldRenderPosX = new float[MaxEntities];
    private readonly float[] _oldRenderPosZ = new float[MaxEntities];
    private readonly int[] _oldProjectileId = new int[MaxEntities];
    private float _interpolationElapsed;
    private int _previousActiveCount;

    public int ActiveCount => _activeCount;
    public int SnapshotsReceived => _snapshotsReceived;

    public override void _Ready()
    {
        if (IsServer)
        {
            _projectileManager = GetParent().GetNodeOrNull<ProjectileManager>("ProjectileManager");
            if (_projectileManager == null)
            {
                NetLog.Error("[ProjectileNetworkSync] Server-side ProjectileManager not found at expected path");
            }
            else
            {
                NetLog.Info("[ProjectileNetworkSync] Server broadcaster ready");
            }
        }
        else
        {
            _clientRenderer = GetNodeOrNull<ProjectileRenderer>("ProjectileRenderer");
            if (_clientRenderer == null)
            {
                NetLog.Error("[ProjectileNetworkSync] Client-side ProjectileRenderer child missing");
            }
            else
            {
                NetLog.Info("[ProjectileNetworkSync] Client receiver ready");
            }
        }
    }

    public override void _Process(double delta)
    {
        if (IsServer)
        {
            if (_projectileManager == null) return;
            _accumulator += (float)delta;
            if (_accumulator < NetworkConfig.SnapshotInterval) return;
            _accumulator -= NetworkConfig.SnapshotInterval;
            BroadcastSnapshot();
        }
        else
        {
            if (_clientRenderer == null) return;

            _interpolationElapsed += (float)delta;
            float t = InterpolationStartCalculator.ComputeAlpha(
                _interpolationElapsed, NetworkConfig.SnapshotInterval);
            for (int i = 0; i < _activeCount; i++)
            {
                _renderPosX[i] = InterpolationStartCalculator.Lerp(_startPosX[i], _posX[i], t);
                _renderPosZ[i] = InterpolationStartCalculator.Lerp(_startPosZ[i], _posZ[i], t);
            }

            _clientRenderer.UpdateInstances(
                _renderPosX, _renderPosZ, _dirX, _dirZ,
                _lifetime, _ownerId, _colorIdx, _activeCount);
        }
    }

    private void BroadcastSnapshot()
    {
        _projectileManager.GetSoAReference(
            out var posX, out var posZ, out var dirX, out var dirZ,
            out var lifetime, out var ownerId, out var colorIdx, out var projectileId);

        int count = _projectileManager.ProjectileCount;
        int needed = ProjectileSnapshot.CalculateBufferSize(count);
        var packet = new byte[needed];
        ProjectileSnapshot.Encode(packet, posX, posZ, dirX, dirZ,
            lifetime, ownerId, colorIdx, projectileId, count);
        Rpc(MethodName.ApplySnapshot, packet);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,
         CallLocal = false,
         TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered,
         TransferChannel = NetworkConfig.ChannelProjectile)]
    private void ApplySnapshot(byte[] data)
    {
        Array.Copy(_renderPosX, _oldRenderPosX, _previousActiveCount);
        Array.Copy(_renderPosZ, _oldRenderPosZ, _previousActiveCount);
        Array.Copy(_previousProjectileId, _oldProjectileId, _previousActiveCount);
        int oldCount = _previousActiveCount;

        int newCount = ProjectileSnapshot.Decode(data, data.Length,
            _posX, _posZ, _dirX, _dirZ,
            _lifetime, _ownerId, _colorIdx, _projectileId);

        InterpolationIdMatcher.BuildIdToIndex(_oldProjectileId, oldCount, _idToOldIndex);
        for (int i = 0; i < newCount; i++)
        {
            int oldIdx = InterpolationIdMatcher.FindOldIndex(_projectileId[i], _idToOldIndex);
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

        Array.Copy(_projectileId, _previousProjectileId, newCount);
        _previousActiveCount = newCount;
        _activeCount = newCount;
        _interpolationElapsed = 0f;
        _snapshotsReceived++;
    }
}
