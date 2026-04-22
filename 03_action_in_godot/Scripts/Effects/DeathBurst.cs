namespace ArenaSurvivor;

using Godot;

public partial class DeathBurst : GpuParticles3D
{
    public static DeathBurst Create(Vector3 position, Color color, float scale, Node parent)
    {
        var effect = new DeathBurst();
        effect.Position = position;
        effect.Emitting = true;
        effect.OneShot = true;
        effect.Amount = 24;
        effect.Lifetime = 0.8f;
        effect.SpeedScale = 1.5f;

        var material = new ParticleProcessMaterial();
        material.Direction = new Vector3(0, 1, 0);
        material.Spread = 180.0f;
        material.InitialVelocityMin = 2.0f;
        material.InitialVelocityMax = 6.0f;
        material.Gravity = new Vector3(0, -8, 0);
        material.ScaleMin = 0.08f * scale;
        material.ScaleMax = 0.25f * scale;
        material.Color = color;
        material.DampingMin = 2.0f;
        material.DampingMax = 5.0f;
        effect.ProcessMaterial = material;

        var mesh = new SphereMesh();
        mesh.Radius = 0.08f;
        mesh.Height = 0.16f;
        effect.DrawPass1 = mesh;

        parent.AddChild(effect);
        effect.Finished += effect.QueueFree;

        return effect;
    }
}
