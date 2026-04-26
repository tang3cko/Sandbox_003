namespace SwarmSurvivor;

using Godot;

public partial class ScreenShake : Node3D
{
    private Camera3D _camera;
    private Vector3 _originalPosition;
    private float _shakeTimer;
    private float _shakeIntensity;

    public override void _Ready()
    {
        _camera = GetViewport().GetCamera3D();
        if (_camera != null)
        {
            _originalPosition = _camera.Position;
        }
    }

    public void Shake(float intensity, float duration)
    {
        _shakeIntensity = intensity;
        _shakeTimer = duration;
    }

    public override void _Process(double delta)
    {
        if (_camera == null) return;

        if (_shakeTimer > 0f)
        {
            _shakeTimer -= (float)delta;
            float strength = _shakeIntensity * (_shakeTimer / 0.2f);
            var offset = new Vector3(
                (float)GD.RandRange(-strength, strength),
                (float)GD.RandRange(-strength, strength),
                0f);
            _camera.Position = _originalPosition + offset;
        }
        else
        {
            _camera.Position = _originalPosition;
        }
    }
}
