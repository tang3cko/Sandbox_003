namespace SwarmSurvivor;

using Godot;

public partial class ProjectileRenderer : MultiMeshInstance3D
{
    private const int MaxInstances = 200;
    private ShaderMaterial _material;

    private static readonly Color[] Palette =
    {
        new(1f, 1f, 0f),       // Yellow
        new(0f, 1f, 1f),       // Cyan
        new(1f, 0f, 1f),       // Magenta
        new(1f, 0.65f, 0f),    // Orange
        new(0.5f, 1f, 0f),     // Lime
        new(1f, 0f, 0f),       // Red
        new(1f, 1f, 1f),       // White
        new(0f, 1f, 1f),       // Aqua
    };

    public override void _Ready()
    {
        var capsule = new CapsuleMesh { Radius = 0.1f, Height = 0.3f };

        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseCustomData = true,
            UseColors = true,
            Mesh = capsule,
            InstanceCount = MaxInstances,
            VisibleInstanceCount = 0,
        };

        Multimesh = multiMesh;

        _material = new ShaderMaterial();
        var shader = GD.Load<Shader>("res://Shaders/projectile.gdshader");
        if (shader != null)
        {
            _material.Shader = shader;
        }
        else
        {
            NetLog.Error("ProjectileRenderer: Failed to load projectile.gdshader");
        }

        capsule.SurfaceSetMaterial(0, _material);
    }

    public void UpdateInstances(
        float[] posX, float[] posZ,
        float[] dirX, float[] dirZ,
        float[] lifetime,
        int[] ownerId,
        int[] colorIdx,
        int count)
    {
        if (Multimesh == null) return;

        Multimesh.VisibleInstanceCount = count;

        for (int i = 0; i < count; i++)
        {
            var transform = Transform3D.Identity;
            transform.Origin = new Vector3(posX[i], 0.5f, posZ[i]);

            float dx = dirX[i];
            float dz = dirZ[i];
            if (dx * dx + dz * dz > 0.01f)
            {
                float angle = Mathf.Atan2(dx, dz);
                transform.Basis = Basis.Identity.Rotated(Vector3.Up, angle);
            }

            Multimesh.SetInstanceTransform(i, transform);

            int paletteIndex = ProjectilePaletteCalculator.WrapPaletteIndex(colorIdx[i], Palette.Length);
            Multimesh.SetInstanceColor(i, Palette[paletteIndex]);

            Multimesh.SetInstanceCustomData(i,
                new Color(dx, dz, lifetime[i], 0f));
        }
    }
}
