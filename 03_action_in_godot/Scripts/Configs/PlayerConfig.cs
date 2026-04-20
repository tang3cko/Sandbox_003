namespace ArenaSurvivor;
using Godot;

[GlobalClass]
public partial class PlayerConfig : Resource
{
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public float MaxStamina { get; set; } = 100f;
    [Export] public float StaminaRegenRate { get; set; } = 20f;
    [Export] public float MoveSpeed { get; set; } = 8.0f;
    [Export] public float DodgeSpeed { get; set; } = 20.0f;
    [Export] public float DodgeDuration { get; set; } = 0.4f;
    [Export] public float DodgeStaminaCost { get; set; } = 25f;
    [Export] public float DodgeCooldown { get; set; } = 0.2f;
    [Export] public float ComboWindow { get; set; } = 0.8f;
    [Export] public float AttackDuration { get; set; } = 0.35f;
    [Export] public float Gravity { get; set; } = 30.0f;
    [Export] public float RotationSpeed { get; set; } = 12.0f;
}
