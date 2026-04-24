namespace SwarmSurvivor;

using Godot;
using ReactiveSO;

public partial class XPCollector : Node3D
{
    [Export] private IntEventChannel _onXPCollected;
    [Export] private IntEventChannel _onLevelUp;
    [Export] private IntVariable _level;
    [Export] private IntVariable _xp;
    [Export] private IntVariable _xpToNext;
    [Export] public int BaseXP { get; set; } = 15;
    [Export] public float GrowthFactor { get; set; } = 1.6f;

    private PlayerProgression _progression;

    public override void _Ready()
    {
        _progression = UpgradeCalculator.CreateInitial(BaseXP);
        UpdateVariables();

        if (_onXPCollected != null)
        {
            _onXPCollected.Raised += HandleXPCollected;
        }
    }

    private void HandleXPCollected(int amount)
    {
        var result = UpgradeCalculator.GainXP(_progression, amount, BaseXP, GrowthFactor);
        _progression = result.State;

        UpdateVariables();

        if (result.DidLevelUp)
        {
            _onLevelUp?.Raise(_progression.Level);
        }
    }

    private void UpdateVariables()
    {
        if (_level != null) _level.Value = _progression.Level;
        if (_xp != null) _xp.Value = _progression.CurrentXP;
        if (_xpToNext != null) _xpToNext.Value = _progression.XPToNextLevel;
    }

    public override void _ExitTree()
    {
        if (_onXPCollected != null)
        {
            _onXPCollected.Raised -= HandleXPCollected;
        }
    }
}
