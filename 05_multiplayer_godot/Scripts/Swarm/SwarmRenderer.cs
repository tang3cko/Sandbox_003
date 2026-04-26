namespace SwarmSurvivor;

using Godot;

public partial class SwarmRenderer : MultiMeshInstance3D
{
    private const int MaxInstances = 600;
    private ShaderMaterial _material;

    public override void _Ready()
    {
        var capsule = new CapsuleMesh { Radius = 0.4f, Height = 0.8f };

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
        var shader = GD.Load<Shader>("res://Shaders/swarm_enemy.gdshader");
        if (shader != null)
        {
            _material.Shader = shader;
        }
        else
        {
            GD.PrintErr("SwarmRenderer: Failed to load swarm_enemy.gdshader");
        }

        capsule.SurfaceSetMaterial(0, _material);
    }

    public void UpdateInstances(
        float[] posX, float[] posZ,
        float[] velX, float[] velZ,
        int[] enemyTypeIndex,
        float[] damageFlashTimer,
        float[] deathTimer,
        int count,
        EnemyTypeConfig[] enemyTypes)
    {
        if (Multimesh == null) return;

        Multimesh.VisibleInstanceCount = count;

        for (int i = 0; i < count; i++)
        {
            var transform = Transform3D.Identity;
            transform.Origin = new Vector3(posX[i], 0f, posZ[i]);

            float vx = velX[i];
            float vz = velZ[i];
            if (vx * vx + vz * vz > 0.01f)
            {
                float angle = Mathf.Atan2(vx, vz);
                transform.Basis = Basis.Identity.Rotated(Vector3.Up, angle);
            }

            Multimesh.SetInstanceTransform(i, transform);

            float flashNorm = damageFlashTimer[i] / SwarmCalculator.DefaultFlashDuration;
            float deathProgress = SwarmCalculator.IsDying(deathTimer[i])
                ? SwarmCalculator.NormalizeDeathProgress(deathTimer[i], SwarmCalculator.DefaultDeathDuration)
                : 0f;
            float typeIdx = (float)enemyTypeIndex[i];

            Multimesh.SetInstanceCustomData(i,
                new Color(flashNorm, deathProgress, typeIdx, 0f));

            if (enemyTypes != null && enemyTypeIndex[i] < enemyTypes.Length)
            {
                Multimesh.SetInstanceColor(i, enemyTypes[enemyTypeIndex[i]].BaseColor);
            }
        }
    }
}
