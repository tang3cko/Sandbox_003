namespace ArenaSurvivor;

using Godot;

public partial class HitEffect : GpuParticles3D
{
    public static HitEffect Create(Vector3 position, Color color, Node parent)
    {
        var effect = new HitEffect();
        effect.Position = position;
        effect.Emitting = true;
        effect.OneShot = true;
        effect.Amount = 12;
        effect.Lifetime = 0.4f;
        effect.SpeedScale = 2.0f;

        var material = new ParticleProcessMaterial();
        material.Direction = new Vector3(0, 1, 0);
        material.Spread = 60.0f;
        material.InitialVelocityMin = 3.0f;
        material.InitialVelocityMax = 8.0f;
        material.Gravity = new Vector3(0, -15, 0);
        material.ScaleMin = 0.05f;
        material.ScaleMax = 0.15f;
        material.Color = color;
        effect.ProcessMaterial = material;

        var mesh = new SphereMesh();
        mesh.Radius = 0.05f;
        mesh.Height = 0.1f;
        effect.DrawPass1 = mesh;

        parent.AddChild(effect);

        // Auto-destroy after lifetime
        effect.Finished += effect.QueueFree;

        return effect;
    }
}
