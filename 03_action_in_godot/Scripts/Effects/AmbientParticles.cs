namespace ArenaSurvivor;

using Godot;

public partial class AmbientParticles : GpuParticles3D
{
    public override void _Ready()
    {
        Amount = 40;
        Lifetime = 6.0f;
        SpeedScale = 0.5f;
        Explosiveness = 0.0f;
        Emitting = true;

        var mat = new ParticleProcessMaterial();
        mat.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box;
        mat.EmissionBoxExtents = new Vector3(20, 0.5f, 20);
        mat.Direction = new Vector3(0, 1, 0);
        mat.Spread = 30.0f;
        mat.InitialVelocityMin = 0.2f;
        mat.InitialVelocityMax = 0.5f;
        mat.Gravity = new Vector3(0.1f, 0.05f, 0);
        mat.ScaleMin = 0.02f;
        mat.ScaleMax = 0.06f;
        mat.Color = new Color(0.8f, 0.7f, 0.5f, 0.3f);
        ProcessMaterial = mat;

        var mesh = new SphereMesh();
        mesh.Radius = 0.5f;
        mesh.Height = 1.0f;
        DrawPass1 = mesh;

        Position = new Vector3(0, 1.5f, 0);
    }
}
