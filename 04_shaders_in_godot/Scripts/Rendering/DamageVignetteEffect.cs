namespace SwarmSurvivor;

using Godot;
using ReactiveSO;

public partial class DamageVignetteEffect : CanvasLayer
{
    [Export] private IntEventChannel _onPlayerDamaged;

    private ColorRect _overlay;
    private float _intensity;
    private const float FadeSpeed = 2f;

    public override void _Ready()
    {
        _overlay = new ColorRect
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            Color = new Color(1f, 0f, 0f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        var material = new ShaderMaterial();
        material.Shader = CreateVignetteShader();
        _overlay.Material = material;

        AddChild(_overlay);

        if (_onPlayerDamaged != null)
        {
            _onPlayerDamaged.Raised += HandleDamage;
        }
    }

    private Shader CreateVignetteShader()
    {
        var shader = new Shader();
        shader.Code = @"
shader_type canvas_item;
uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform vec3 vignette_color : source_color = vec3(0.8, 0.0, 0.0);

void fragment() {
    vec2 uv = UV * 2.0 - 1.0;
    float dist = length(uv);
    float vignette = smoothstep(0.3, 1.2, dist);
    COLOR = vec4(vignette_color, vignette * intensity);
}
";
        return shader;
    }

    private void HandleDamage(int health)
    {
        _intensity = 0.6f;
    }

    public override void _Process(double delta)
    {
        if (_intensity > 0f)
        {
            _intensity -= FadeSpeed * (float)delta;
            if (_intensity < 0f) _intensity = 0f;
        }

        var material = _overlay.Material as ShaderMaterial;
        material?.SetShaderParameter("intensity", _intensity);
    }

    public override void _ExitTree()
    {
        if (_onPlayerDamaged != null)
        {
            _onPlayerDamaged.Raised -= HandleDamage;
        }
    }
}
