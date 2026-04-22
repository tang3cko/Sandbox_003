namespace ArenaSurvivor;

using Godot;

public partial class MovementDust : GpuParticles3D
{
    private CharacterBody3D _body;

    public override void _Ready()
    {
        _body = GetParent<CharacterBody3D>();

        Amount = 8;
        Lifetime = 0.6f;
        SpeedScale = 1.5f;
        Explosiveness = 0.0f;
        Emitting = false;

        var mat = new ParticleProcessMaterial();
        mat.Direction = new Vector3(0, 1, 0);
        mat.Spread = 45.0f;
        mat.InitialVelocityMin = 0.5f;
        mat.InitialVelocityMax = 1.5f;
        mat.Gravity = new Vector3(0, -3, 0);
        mat.ScaleMin = 0.05f;
        mat.ScaleMax = 0.15f;
        mat.Color = new Color(0.6f, 0.55f, 0.45f, 0.5f);
        ProcessMaterial = mat;

        var mesh = new SphereMesh();
        mesh.Radius = 0.5f;
        mesh.Height = 1.0f;
        DrawPass1 = mesh;

        Position = new Vector3(0, 0.05f, 0);
    }

    public override void _Process(double delta)
    {
        if (_body == null) return;

        var vel = _body.Velocity;
        var speed = new Vector2(vel.X, vel.Z).Length();
        Emitting = speed > 1.0f;
    }
}
