using Godot;
using VoxelTerrain.Player;
using VoxelTerrain.World;

namespace VoxelTerrain;

public partial class Main : Node3D
{
    public override void _Ready()
    {
        AddDirectionalLight();
        AddEnvironment();

        var world = new VoxelWorld
        {
            Name = "World",
            SampleSize = 56,
            GroundLevel = 22f,
            HillAmplitude = 6f,
            HillFrequency = 0.05f,
        };
        AddChild(world);

        var player = new FreeCameraPlayer
        {
            Name = "Player",
            Position = new Vector3(28, 32, 70),
        };
        AddChild(player);

        var tool_ = new DiggingTool
        {
            Name = "DiggingTool",
            CameraPath = player.GetPath() + "/Camera",
            WorldPath = world.GetPath(),
        };
        AddChild(tool_);

        AddCrosshair();
    }

    private void AddDirectionalLight()
    {
        var light = new DirectionalLight3D
        {
            Name = "Sun",
            ShadowEnabled = true,
        };
        light.RotateX(-Mathf.Pi / 3.5f);
        light.RotateY(-Mathf.Pi / 6f);
        AddChild(light);
    }

    private void AddEnvironment()
    {
        var env = new WorldEnvironment
        {
            Name = "Env",
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Sky,
                Sky = new Sky
                {
                    SkyMaterial = new ProceduralSkyMaterial(),
                },
                AmbientLightSource = Godot.Environment.AmbientSource.Sky,
                AmbientLightEnergy = 0.6f,
            },
        };
        AddChild(env);
    }

    private void AddCrosshair()
    {
        var ui = new CanvasLayer { Name = "UI" };
        AddChild(ui);
        var label = new Label
        {
            Name = "Crosshair",
            Text = "+",
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -6,
            OffsetTop = -10,
        };
        ui.AddChild(label);

        var hint = new Label
        {
            Name = "Hint",
            Text = "WASD: move  Space/Shift: up/down  LMB: dig  RMB: place  Esc: free mouse",
            AnchorTop = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 12,
            OffsetTop = -28,
            OffsetRight = 800,
            OffsetBottom = -8,
        };
        ui.AddChild(hint);
    }
}
