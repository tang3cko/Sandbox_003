namespace ArenaSurvivor;

using Godot;
using ReactiveSO;

public partial class AudioManager : Node3D
{
    [Export] private VoidEventChannel _onPlayerAttacked;
    [Export] private VoidEventChannel _onPlayerDodged;
    [Export] private IntEventChannel _onPlayerDamaged;
    [Export] private VoidEventChannel _onEnemyKilled;

    private AudioStreamPlayer3D _attackSound;
    private AudioStreamPlayer3D _dodgeSound;
    private AudioStreamPlayer3D _hitSound;
    private AudioStreamPlayer3D _killSound;
    private AudioStreamPlayer _ambientSound;

    public override void _Ready()
    {
        _attackSound = CreateSpatialPlayer(1.0f, 0.8f);
        _dodgeSound = CreateSpatialPlayer(0.7f, 1.2f);
        _hitSound = CreateSpatialPlayer(0.9f, 0.6f);
        _killSound = CreateSpatialPlayer(1.0f, 1.5f);
        _ambientSound = CreateAmbientPlayer(0.3f, 1.0f);

        _onPlayerAttacked.Raised += HandleAttack;
        _onPlayerDodged.Raised += HandleDodge;
        _onPlayerDamaged.Raised += HandleDamaged;
        _onEnemyKilled.Raised += HandleKill;

        PlayAmbientLoop();
    }

    private void HandleAttack() => PlaySynthSound(_attackSound, 220.0f, 0.08f);
    private void HandleDodge() => PlaySynthSound(_dodgeSound, 440.0f, 0.05f);
    private void HandleDamaged(int _) => PlaySynthSound(_hitSound, 110.0f, 0.12f);
    private void HandleKill() => PlaySynthSound(_killSound, 660.0f, 0.1f);

    private void PlaySynthSound(AudioStreamPlayer3D player, float frequency, float duration)
    {
        var generator = new AudioStreamGenerator();
        generator.MixRate = 44100;
        generator.BufferLength = duration + 0.05f;
        player.Stream = generator;
        player.Play();

        var playback = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        if (playback == null) return;

        int frames = (int)(44100 * duration);
        for (int i = 0; i < frames; i++)
        {
            float t = (float)i / 44100;
            float envelope = 1.0f - (t / duration);
            float sample = Mathf.Sin(t * frequency * Mathf.Tau) * envelope * 0.3f;
            playback.PushFrame(new Vector2(sample, sample));
        }
    }

    private void PlayAmbientLoop()
    {
        var generator = new AudioStreamGenerator();
        generator.MixRate = 44100;
        generator.BufferLength = 0.5f;
        _ambientSound.Stream = generator;
        _ambientSound.Play();
    }

    private AudioStreamPlayer3D CreateSpatialPlayer(float volumeDb, float pitchScale)
    {
        var player = new AudioStreamPlayer3D();
        player.VolumeDb = Mathf.LinearToDb(volumeDb);
        player.PitchScale = pitchScale;
        player.MaxDistance = 50.0f;
        AddChild(player);
        return player;
    }

    private AudioStreamPlayer CreateAmbientPlayer(float volumeDb, float pitchScale)
    {
        var player = new AudioStreamPlayer();
        player.VolumeDb = Mathf.LinearToDb(volumeDb);
        player.PitchScale = pitchScale;
        AddChild(player);
        return player;
    }

    public override void _ExitTree()
    {
        _onPlayerAttacked.Raised -= HandleAttack;
        _onPlayerDodged.Raised -= HandleDodge;
        _onPlayerDamaged.Raised -= HandleDamaged;
        _onEnemyKilled.Raised -= HandleKill;
    }
}
