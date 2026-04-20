namespace Collector;

using Godot;

public partial class HUD : CanvasLayer
{
    private Label scoreLabel;
    private Label livesLabel;
    private Label waveLabel;
    private Label gameOverLabel;

    [Export] public IntEventChannel OnScoreChanged { get; private set; }
    [Export] public IntEventChannel OnLivesChanged { get; private set; }
    [Export] public IntEventChannel OnWaveCleared { get; private set; }
    [Export] public VoidEventChannel OnGameOver { get; private set; }

    public override void _Ready()
    {
        scoreLabel = GetNode<Label>("ScoreLabel");
        livesLabel = GetNode<Label>("LivesLabel");
        waveLabel = GetNode<Label>("WaveLabel");
        gameOverLabel = GetNode<Label>("GameOverLabel");

        OnScoreChanged.Raised += HandleScoreChanged;
        OnLivesChanged.Raised += HandleLivesChanged;
        OnWaveCleared.Raised += HandleWaveCleared;
        OnGameOver.Raised += HandleGameOver;
    }

    private void HandleScoreChanged(int score)
    {
        scoreLabel.Text = $"Score: {score}";
    }

    private void HandleLivesChanged(int lives)
    {
        livesLabel.Text = $"Lives: {lives}";
    }

    private void HandleWaveCleared(int wave)
    {
        waveLabel.Text = $"Wave: {wave}";
        gameOverLabel.Visible = false;
    }

    private void HandleGameOver()
    {
        gameOverLabel.Visible = true;
        gameOverLabel.Text = "GAME OVER\nPress R to Restart";
    }
}
