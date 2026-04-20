namespace ArenaSurvivor;

using Godot;

public partial class ScreenShake : Node
{
    private Camera3D _camera;
    private Vector3 _originalPosition;
    private float _shakeTimer;
    private float _shakeIntensity;
    private RandomNumberGenerator _rng;

    public void SetCamera(Camera3D camera)
    {
        _camera = camera;
        _originalPosition = camera.Position;
    }

    public override void _Ready()
    {
        _rng = new RandomNumberGenerator();
        _rng.Randomize();
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
            var offset = new Vector3(
                _rng.RandfRange(-1f, 1f) * _shakeIntensity,
                _rng.RandfRange(-1f, 1f) * _shakeIntensity,
                0
            );
            _camera.Position = _originalPosition + offset;

            if (_shakeTimer <= 0f)
            {
                _camera.Position = _originalPosition;
            }
        }
    }
}
