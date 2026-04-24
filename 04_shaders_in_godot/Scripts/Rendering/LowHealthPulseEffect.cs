namespace SwarmSurvivor;

using Godot;
using ReactiveSO;

public partial class LowHealthPulseEffect : CanvasLayer
{
    [Export] private IntVariable _health;
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public float LowHealthThreshold { get; set; } = 0.3f;

    private ColorRect _overlay;

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
        material.Shader = CreatePulseShader();
        _overlay.Material = material;

        AddChild(_overlay);
    }

    private Shader CreatePulseShader()
    {
        var shader = new Shader();
        shader.Code = @"
shader_type canvas_item;
uniform float health_ratio : hint_range(0.0, 1.0) = 1.0;
uniform float low_threshold : hint_range(0.0, 1.0) = 0.3;
uniform float time = 0.0;

void fragment() {
    float danger = 0.0;
    if (health_ratio <= low_threshold && low_threshold > 0.0) {
        danger = 1.0 - health_ratio / low_threshold;
    }
    float pulse = 0.5 + 0.5 * sin(time * 3.0);
    vec2 uv = UV * 2.0 - 1.0;
    float dist = length(uv);
    float vignette = smoothstep(0.5, 1.3, dist);
    float alpha = vignette * danger * pulse * 0.4;
    COLOR = vec4(0.6, 0.0, 0.0, alpha);
}
";
        return shader;
    }

    public override void _Process(double delta)
    {
        float healthRatio = 1f;
        if (_health != null && MaxHealth > 0)
        {
            healthRatio = (float)_health.Value / MaxHealth;
        }

        var material = _overlay.Material as ShaderMaterial;
        if (material != null)
        {
            material.SetShaderParameter("health_ratio", healthRatio);
            material.SetShaderParameter("low_threshold", LowHealthThreshold);
            material.SetShaderParameter("time", (float)Time.GetTicksMsec() / 1000f);
        }
    }
}
