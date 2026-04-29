namespace SwarmSurvivor;

using Godot;

public partial class MuzzleFlashFX : Node3D
{
    private const float Duration = 0.12f;
    private const float PeakTime = 0.04f;

    private MeshInstance3D _mesh;
    private StandardMaterial3D _material;
    private OmniLight3D _light;
    private Color _color;

    public static void Play(Node parent, Vector3 worldPos, Color color)
    {
        if (parent == null) return;

        var fx = new MuzzleFlashFX();
        fx._color = color;
        parent.AddChild(fx);
        fx.GlobalPosition = worldPos;
        fx.StartFlash();
    }

    public override void _Ready()
    {
        _material = new StandardMaterial3D
        {
            EmissionEnabled = true,
            Emission = _color * 4f,
            AlbedoColor = _color,
        };

        var sphere = new SphereMesh
        {
            Radius = 0.25f,
            Height = 0.5f,
        };
        sphere.SurfaceSetMaterial(0, _material);

        _mesh = new MeshInstance3D
        {
            Mesh = sphere,
        };
        AddChild(_mesh);

        _light = new OmniLight3D
        {
            LightColor = _color,
            OmniRange = 2f,
            LightEnergy = 2f,
        };
        AddChild(_light);

        Scale = Vector3.One * 0.3f;
    }

    private void StartFlash()
    {
        if (_mesh == null) return;

        var scaleTween = CreateTween();
        scaleTween.TweenProperty(this, "scale", Vector3.One * 1.2f, PeakTime);
        scaleTween.TweenProperty(this, "scale", Vector3.One * 0.1f, Duration - PeakTime);

        var emissionTween = CreateTween();
        emissionTween.TweenProperty(_material, "emission", _color * 0f, Duration);

        var lightTween = CreateTween();
        lightTween.TweenProperty(_light, "light_energy", 0f, Duration);

        scaleTween.Finished += OnTweenFinished;
    }

    private void OnTweenFinished()
    {
        QueueFree();
    }
}
