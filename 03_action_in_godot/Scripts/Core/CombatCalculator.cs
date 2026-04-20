using System;

namespace ArenaSurvivor;

public struct PlayerState
{
    public int Health;
    public int MaxHealth;
    public float Stamina;
    public float MaxStamina;
    public bool IsInvincible;
    public bool IsDodging;
    public bool IsAttacking;
    public int ComboCount;
    public float ComboTimer;
}

public readonly record struct DamageResult(PlayerState State, bool IsDead);

public readonly record struct DodgeResult(PlayerState State, bool CanDodge);

public readonly record struct AttackResult(PlayerState State, int Damage, bool IsComboFinisher);

public readonly record struct StaminaRegenResult(PlayerState State);

public static class CombatCalculator
{
    public static PlayerState CreateInitial(int maxHealth, float maxStamina)
    {
        return new PlayerState
        {
            Health = maxHealth,
            MaxHealth = maxHealth,
            Stamina = maxStamina,
            MaxStamina = maxStamina,
            IsInvincible = false,
            IsDodging = false,
            IsAttacking = false,
            ComboCount = 0,
            ComboTimer = 0f,
        };
    }

    public static DamageResult TakeDamage(PlayerState state, int damage)
    {
        if (state.IsInvincible || state.IsDodging)
        {
            return new DamageResult(state, false);
        }

        var newHealth = Math.Max(0, state.Health - damage);
        state.Health = newHealth;
        return new DamageResult(state, newHealth <= 0);
    }

    public static DodgeResult TryDodge(PlayerState state, float staminaCost)
    {
        if (state.Stamina < staminaCost || state.IsDodging || state.IsAttacking)
        {
            return new DodgeResult(state, false);
        }

        state.Stamina -= staminaCost;
        state.IsDodging = true;
        return new DodgeResult(state, true);
    }

    public static PlayerState EndDodge(PlayerState state)
    {
        state.IsDodging = false;
        state.IsInvincible = false;
        return state;
    }

    public static AttackResult TryAttack(PlayerState state, int baseDamage, float comboWindow)
    {
        if (state.IsDodging)
        {
            return new AttackResult(state, 0, false);
        }

        int comboCount;
        if (state.ComboTimer > 0f)
        {
            comboCount = Math.Min(state.ComboCount + 1, 3);
        }
        else
        {
            comboCount = 1;
        }

        bool isFinisher = comboCount >= 3;
        int damage = isFinisher ? baseDamage * 2 : baseDamage;

        if (isFinisher)
        {
            state.ComboCount = 0;
            state.ComboTimer = 0f;
        }
        else
        {
            state.ComboCount = comboCount;
            state.ComboTimer = comboWindow;
        }

        state.IsAttacking = true;
        return new AttackResult(state, damage, isFinisher);
    }

    public static PlayerState EndAttack(PlayerState state)
    {
        state.IsAttacking = false;
        return state;
    }

    public static StaminaRegenResult RegenStamina(PlayerState state, float amount)
    {
        state.Stamina = Math.Min(state.MaxStamina, state.Stamina + amount);
        return new StaminaRegenResult(state);
    }

    public static PlayerState UpdateComboTimer(PlayerState state, float deltaTime)
    {
        if (state.ComboTimer <= 0f)
        {
            return state;
        }

        state.ComboTimer = Math.Max(0f, state.ComboTimer - deltaTime);
        if (state.ComboTimer <= 0f)
        {
            state.ComboCount = 0;
        }

        return state;
    }
}
