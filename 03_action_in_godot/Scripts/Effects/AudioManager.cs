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
    private AudioStreamPlayer _bgmPlayer;
    private AudioStreamPlayer _ambientPlayer;

    public override void _Ready()
    {
        _attackSound = CreateSpatialPlayer("SFX", 0.8f, 30.0f);
        _dodgeSound = CreateSpatialPlayer("SFX", 1.2f, 25.0f);
        _hitSound = CreateSpatialPlayer("SFX", 0.6f, 35.0f);
        _killSound = CreateSpatialPlayer("SFX", 1.5f, 40.0f);
        _bgmPlayer = CreateGlobalPlayer("BGM", -6.0f);
        _ambientPlayer = CreateGlobalPlayer("Ambient", -12.0f);

        _onPlayerAttacked.Raised += HandleAttack;
        _onPlayerDodged.Raised += HandleDodge;
        _onPlayerDamaged.Raised += HandleDamaged;
        _onEnemyKilled.Raised += HandleKill;

        PlayBgmLoop();
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
            float envelope = Mathf.Exp(-t / duration * 4.0f);
            float sample = Mathf.Sin(t * frequency * Mathf.Tau) * envelope * 0.3f;
            // Add harmonics for richer sound
            sample += Mathf.Sin(t * frequency * 2 * Mathf.Tau) * envelope * 0.1f;
            sample += Mathf.Sin(t * frequency * 3 * Mathf.Tau) * envelope * 0.05f;
            playback.PushFrame(new Vector2(sample, sample));
        }
    }

    private void PlayBgmLoop()
    {
        var generator = new AudioStreamGenerator();
        generator.MixRate = 44100;
        generator.BufferLength = 0.5f;
        _bgmPlayer.Stream = generator;
        _bgmPlayer.Play();
    }

    private void PlayAmbientLoop()
    {
        var generator = new AudioStreamGenerator();
        generator.MixRate = 44100;
        generator.BufferLength = 0.5f;
        _ambientPlayer.Stream = generator;
        _ambientPlayer.Play();
    }

    private AudioStreamPlayer3D CreateSpatialPlayer(string bus, float pitchScale, float maxDistance)
    {
        var player = new AudioStreamPlayer3D();
        player.Bus = bus;
        player.PitchScale = pitchScale;
        player.MaxDistance = maxDistance;
        player.AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseSquareDistance;
        player.UnitSize = 5.0f;
        AddChild(player);
        return player;
    }

    private AudioStreamPlayer CreateGlobalPlayer(string bus, float volumeDb)
    {
        var player = new AudioStreamPlayer();
        player.Bus = bus;
        player.VolumeDb = volumeDb;
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
