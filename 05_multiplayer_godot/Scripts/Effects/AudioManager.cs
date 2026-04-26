namespace SwarmSurvivor;

using Godot;
using ReactiveSO;

public partial class AudioManager : Node
{
    [Export] private VoidEventChannel _onEnemyKilled;
    [Export] private IntEventChannel _onPlayerDamaged;
    [Export] private IntEventChannel _onLevelUp;
    [Export] private IntEventChannel _onXPCollected;

    private AudioStreamPlayer _sfxPlayer;

    public override void _Ready()
    {
        _sfxPlayer = new AudioStreamPlayer { Bus = "SFX" };
        AddChild(_sfxPlayer);

        if (_onEnemyKilled != null) _onEnemyKilled.Raised += HandleEnemyKilled;
        if (_onPlayerDamaged != null) _onPlayerDamaged.Raised += HandlePlayerDamaged;
        if (_onLevelUp != null) _onLevelUp.Raised += HandleLevelUp;
        if (_onXPCollected != null) _onXPCollected.Raised += HandleXPCollected;
    }

    private void HandleEnemyKilled() => PlayTone(660f, 0.05f);
    private void HandlePlayerDamaged(int _) => PlayTone(110f, 0.1f);
    private void HandleLevelUp(int _) => PlayTone(880f, 0.15f);
    private void HandleXPCollected(int _) => PlayTone(1320f, 0.03f);

    public override void _ExitTree()
    {
        if (_onEnemyKilled != null) _onEnemyKilled.Raised -= HandleEnemyKilled;
        if (_onPlayerDamaged != null) _onPlayerDamaged.Raised -= HandlePlayerDamaged;
        if (_onLevelUp != null) _onLevelUp.Raised -= HandleLevelUp;
        if (_onXPCollected != null) _onXPCollected.Raised -= HandleXPCollected;
    }

    private void PlayTone(float frequency, float duration)
    {
        var generator = new AudioStreamGenerator
        {
            MixRate = 22050f,
            BufferLength = duration + 0.05f,
        };

        _sfxPlayer.Stream = generator;
        _sfxPlayer.Play();

        var playback = _sfxPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        if (playback == null) return;

        int samples = (int)(duration * 22050f);
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / 22050f;
            float envelope = 1f - t / duration;
            float sample = Mathf.Sin(t * frequency * Mathf.Tau) * envelope * 0.3f;
            sample += Mathf.Sin(t * frequency * 2f * Mathf.Tau) * envelope * 0.1f;
            playback.PushFrame(new Vector2(sample, sample));
        }
    }
}
