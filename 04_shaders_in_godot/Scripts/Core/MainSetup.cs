namespace SwarmSurvivor;

using Godot;

public partial class MainSetup : Node3D
{
    public override void _Ready()
    {
        SetupArena();
        SetupCamera();
        SetupLighting();
    }

    private void SetupArena()
    {
        var ground = new StaticBody3D();
        var groundMesh = new MeshInstance3D();
        var plane = new PlaneMesh { Size = new Vector2(50f, 50f) };

        var groundMaterial = new ShaderMaterial();
        var shader = GD.Load<Shader>("res://Shaders/arena_ground.gdshader");
        if (shader != null)
        {
            groundMaterial.Shader = shader;
        }
        else
        {
            var fallback = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.1f, 0.12f, 0.15f),
            };
            plane.SurfaceSetMaterial(0, fallback);
        }

        if (shader != null)
        {
            plane.SurfaceSetMaterial(0, groundMaterial);
        }

        groundMesh.Mesh = plane;
        ground.AddChild(groundMesh);

        var groundShape = new CollisionShape3D
        {
            Shape = new WorldBoundaryShape3D(),
        };
        ground.AddChild(groundShape);

        AddChild(ground);
    }

    private void SetupCamera()
    {
        var camera = new Camera3D
        {
            Position = new Vector3(0f, 25f, 15f),
            RotationDegrees = new Vector3(-60f, 0f, 0f),
            Projection = Camera3D.ProjectionType.Perspective,
            Fov = 50f,
        };
        AddChild(camera);
    }

    private void SetupLighting()
    {
        var light = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-45f, -30f, 0f),
            ShadowEnabled = true,
            LightEnergy = 0.8f,
        };
        AddChild(light);

        var env = new WorldEnvironment();
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.02f, 0.03f, 0.05f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.15f, 0.15f, 0.2f),
            AmbientLightEnergy = 0.5f,
        };
        env.Environment = environment;
        AddChild(env);
    }
}
