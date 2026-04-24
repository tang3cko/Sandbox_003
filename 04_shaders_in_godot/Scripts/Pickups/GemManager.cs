namespace SwarmSurvivor;

using Godot;
using ReactiveSO;

public partial class GemManager : Node3D
{
    [Export] private IntEventChannel _onXPCollected;
    [Export] private Vector3EventChannel _onEnemyKilledAt;
    [Export] public PlayerConfig Config { get; set; }

    private const int MaxGems = 1000;
    private const int DefaultGemXP = 3;

    private float[] _posX, _posZ;
    private int[] _xpValue;
    private float[] _lifetime;
    private int _gemCount;

    private MultiMeshInstance3D _renderer;
    private PlayerController _player;

    public override void _Ready()
    {
        _posX = new float[MaxGems];
        _posZ = new float[MaxGems];
        _xpValue = new int[MaxGems];
        _lifetime = new float[MaxGems];
        _gemCount = 0;

        _player = GetNode<PlayerController>("../Player");
        if (Config != null) _magnetRadius = Config.MagnetRadius;

        SetupRenderer();

        if (_onEnemyKilledAt != null)
        {
            _onEnemyKilledAt.Raised += HandleEnemyKilledAt;
        }
    }

    private void HandleEnemyKilledAt(Vector3 position)
    {
        SpawnGem(position.X, position.Z, DefaultGemXP);
    }

    private void SetupRenderer()
    {
        var sphere = new SphereMesh { Radius = 0.15f, Height = 0.3f };

        var material = new ShaderMaterial();
        var shader = GD.Load<Shader>("res://Shaders/xp_gem.gdshader");
        if (shader != null)
        {
            material.Shader = shader;
        }
        sphere.SurfaceSetMaterial(0, material);

        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseCustomData = true,
            UseColors = false,
            Mesh = sphere,
            InstanceCount = MaxGems,
            VisibleInstanceCount = 0,
        };

        _renderer = new MultiMeshInstance3D { Multimesh = multiMesh };
        AddChild(_renderer);
    }

    private void SpawnGem(float x, float z, int xpValue)
    {
        if (_gemCount >= MaxGems) return;

        int i = _gemCount;
        _posX[i] = x;
        _posZ[i] = z;
        _xpValue[i] = xpValue;
        _lifetime[i] = 0f;
        _gemCount++;
    }

    public override void _Process(double delta)
    {
        if (_player == null || Config == null) return;

        float dt = (float)delta;
        float playerX = _player.GlobalPosition.X;
        float playerZ = _player.GlobalPosition.Z;

        float collectSq = Config.CollectRadius * Config.CollectRadius;
        float magnetSq = _magnetRadius * _magnetRadius;

        for (int i = _gemCount - 1; i >= 0; i--)
        {
            _lifetime[i] += dt;

            float distSq = SwarmCalculator.DistanceSquared(
                _posX[i], _posZ[i], playerX, playerZ);

            if (distSq < collectSq)
            {
                _onXPCollected?.Raise(_xpValue[i]);
                RemoveGem(i);
                continue;
            }

            if (distSq < magnetSq)
            {
                float dx = playerX - _posX[i];
                float dz = playerZ - _posZ[i];
                float dist = Mathf.Sqrt(distSq);
                if (dist > 0.01f)
                {
                    _posX[i] += dx / dist * Config.MagnetSpeed * dt;
                    _posZ[i] += dz / dist * Config.MagnetSpeed * dt;
                }
            }
        }

        UploadToRenderer();
    }

    private float _magnetRadius = 6f;

    public void SetMagnetRadius(float radius)
    {
        _magnetRadius = radius;
    }

    private void RemoveGem(int index)
    {
        int last = _gemCount - 1;
        if (index != last)
        {
            _posX[index] = _posX[last];
            _posZ[index] = _posZ[last];
            _xpValue[index] = _xpValue[last];
            _lifetime[index] = _lifetime[last];
        }
        _gemCount--;
    }

    private void UploadToRenderer()
    {
        if (_renderer?.Multimesh == null) return;

        _renderer.Multimesh.VisibleInstanceCount = _gemCount;

        for (int i = 0; i < _gemCount; i++)
        {
            var transform = Transform3D.Identity;
            transform.Origin = new Vector3(_posX[i], 0.3f, _posZ[i]);
            _renderer.Multimesh.SetInstanceTransform(i, transform);
            _renderer.Multimesh.SetInstanceCustomData(i,
                new Color(_lifetime[i], 0f, 0f, 0f));
        }
    }

    public override void _ExitTree()
    {
        if (_onEnemyKilledAt != null)
        {
            _onEnemyKilledAt.Raised -= HandleEnemyKilledAt;
        }
    }
}
