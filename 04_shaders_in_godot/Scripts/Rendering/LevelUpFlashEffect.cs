namespace SwarmSurvivor;

using Godot;
using ReactiveSO;

public partial class LevelUpFlashEffect : CanvasLayer
{
    [Export] private IntEventChannel _onLevelUp;

    private ColorRect _overlay;
    private float _intensity;
    private const float FadeSpeed = 3f;

    public override void _Ready()
    {
        _overlay = new ColorRect
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            Color = new Color(1f, 0.85f, 0.2f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        var material = new ShaderMaterial();
        material.Shader = CreateFlashShader();
        _overlay.Material = material;

        AddChild(_overlay);

        if (_onLevelUp != null)
        {
            _onLevelUp.Raised += HandleLevelUp;
        }
    }

    private Shader CreateFlashShader()
    {
        var shader = new Shader();
        shader.Code = @"
shader_type canvas_item;
uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform vec3 flash_color : source_color = vec3(1.0, 0.85, 0.2);

void fragment() {
    float alpha = intensity * 0.4;
    // Radial glow from center
    vec2 uv = UV * 2.0 - 1.0;
    float dist = length(uv);
    float glow = 1.0 - smoothstep(0.0, 1.5, dist);
    alpha *= (0.5 + 0.5 * glow);
    COLOR = vec4(flash_color, alpha);
}
";
        return shader;
    }

    private void HandleLevelUp(int level)
    {
        _intensity = 1f;
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
        if (_onLevelUp != null)
        {
            _onLevelUp.Raised -= HandleLevelUp;
        }
    }
}
