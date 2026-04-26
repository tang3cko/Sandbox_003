namespace SwarmSurvivor;

using Godot;

public partial class ProjectileManager : Node3D
{
    private const int MaxProjectiles = 200;
    private const int MaxOrbitals = 8;

    private float[] _projX, _projZ;
    private float[] _dirX, _dirZ;
    private float[] _speed;
    private int[] _damage;
    private float[] _distanceTraveled;
    private float[] _maxRange;
    private Color[] _projColor;
    private int _projCount;

    private float[] _orbX, _orbZ;
    private Color[] _orbColor;
    private int _orbCount;

    private MultiMeshInstance3D _projRenderer;
    private MultiMeshInstance3D _orbRenderer;
    private SwarmManager _swarmManager;

    public override void _Ready()
    {
        _projX = new float[MaxProjectiles];
        _projZ = new float[MaxProjectiles];
        _dirX = new float[MaxProjectiles];
        _dirZ = new float[MaxProjectiles];
        _speed = new float[MaxProjectiles];
        _damage = new int[MaxProjectiles];
        _distanceTraveled = new float[MaxProjectiles];
        _maxRange = new float[MaxProjectiles];
        _projColor = new Color[MaxProjectiles];
        _projCount = 0;

        _orbX = new float[MaxOrbitals];
        _orbZ = new float[MaxOrbitals];
        _orbColor = new Color[MaxOrbitals];
        _orbCount = 0;

        _swarmManager = GetNode<SwarmManager>("../SwarmManager");

        SetupRenderers();
    }

    private void SetupRenderers()
    {
        _projRenderer = CreateMultiMeshRenderer(MaxProjectiles, 0.1f, 0.3f, "projectile");
        AddChild(_projRenderer);

        _orbRenderer = CreateMultiMeshRenderer(MaxOrbitals, 0.25f, 0.5f, "orbital");
        AddChild(_orbRenderer);
    }

    private MultiMeshInstance3D CreateMultiMeshRenderer(int count, float radius, float height, string name)
    {
        var capsule = new CapsuleMesh { Radius = radius, Height = height };

        var material = new ShaderMaterial();
        var shader = GD.Load<Shader>("res://Shaders/projectile.gdshader");
        if (shader != null)
        {
            material.Shader = shader;
        }
        capsule.SurfaceSetMaterial(0, material);

        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseCustomData = true,
            UseColors = true,
            Mesh = capsule,
            InstanceCount = count,
            VisibleInstanceCount = 0,
        };

        return new MultiMeshInstance3D { Multimesh = multiMesh, Name = name };
    }

    public void SpawnProjectile(
        float x, float z, float dirX, float dirZ,
        float speed, int damage, float range, Color color)
    {
        if (_projCount >= MaxProjectiles) return;

        int i = _projCount;
        _projX[i] = x;
        _projZ[i] = z;
        _dirX[i] = dirX;
        _dirZ[i] = dirZ;
        _speed[i] = speed;
        _damage[i] = damage;
        _distanceTraveled[i] = 0f;
        _maxRange[i] = range;
        _projColor[i] = color;
        _projCount++;
    }

    public void UpdateOrbital(int index, float x, float z, Color color)
    {
        if (index >= MaxOrbitals) return;
        _orbX[index] = x;
        _orbZ[index] = z;
        _orbColor[index] = color;
    }

    public void SetOrbitalCount(int count)
    {
        _orbCount = count;
    }

    public (float x, float z) GetOrbitalPosition(int index)
    {
        if (index < 0 || index >= _orbCount) return (0f, 0f);
        return (_orbX[index], _orbZ[index]);
    }

    public int GetOrbitalCount() => _orbCount;

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        UpdateProjectiles(dt);
        UploadProjectiles();
        UploadOrbitals();
    }

    private void UpdateProjectiles(float dt)
    {
        float hitRadius = 0.8f;

        for (int i = _projCount - 1; i >= 0; i--)
        {
            float move = _speed[i] * dt;
            _projX[i] += _dirX[i] * move;
            _projZ[i] += _dirZ[i] * move;
            _distanceTraveled[i] += move;

            if (_distanceTraveled[i] >= _maxRange[i])
            {
                RemoveProjectile(i);
                continue;
            }

            if (_swarmManager != null)
            {
                _swarmManager.DamageInRadius(_projX[i], _projZ[i], hitRadius, _damage[i]);
            }
        }
    }

    private void RemoveProjectile(int index)
    {
        int last = _projCount - 1;
        if (index != last)
        {
            _projX[index] = _projX[last];
            _projZ[index] = _projZ[last];
            _dirX[index] = _dirX[last];
            _dirZ[index] = _dirZ[last];
            _speed[index] = _speed[last];
            _damage[index] = _damage[last];
            _distanceTraveled[index] = _distanceTraveled[last];
            _maxRange[index] = _maxRange[last];
            _projColor[index] = _projColor[last];
        }
        _projCount--;
    }

    private void UploadProjectiles()
    {
        if (_projRenderer?.Multimesh == null) return;

        _projRenderer.Multimesh.VisibleInstanceCount = _projCount;

        for (int i = 0; i < _projCount; i++)
        {
            var transform = Transform3D.Identity;
            transform.Origin = new Vector3(_projX[i], 0.5f, _projZ[i]);

            float angle = Mathf.Atan2(_dirX[i], _dirZ[i]);
            transform.Basis = Basis.Identity.Rotated(Vector3.Up, angle);

            _projRenderer.Multimesh.SetInstanceTransform(i, transform);
            _projRenderer.Multimesh.SetInstanceColor(i, _projColor[i]);
            _projRenderer.Multimesh.SetInstanceCustomData(i,
                new Color(_dirX[i], _dirZ[i], 0f, 0f));
        }
    }

    private void UploadOrbitals()
    {
        if (_orbRenderer?.Multimesh == null) return;

        _orbRenderer.Multimesh.VisibleInstanceCount = _orbCount;

        for (int i = 0; i < _orbCount; i++)
        {
            var transform = Transform3D.Identity;
            transform.Origin = new Vector3(_orbX[i], 0.5f, _orbZ[i]);
            _orbRenderer.Multimesh.SetInstanceTransform(i, transform);
            _orbRenderer.Multimesh.SetInstanceColor(i, _orbColor[i]);
            _orbRenderer.Multimesh.SetInstanceCustomData(i,
                new Color(0f, 0f, 0f, 0f));
        }
    }
}
