using Xunit;

namespace ArenaSurvivor.Tests;

public class CombatCalculatorTest
{
    [Fact]
    public void CreateInitial_SetsAllFieldsCorrectly()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);

        Assert.Equal(100, state.Health);
        Assert.Equal(100, state.MaxHealth);
        Assert.Equal(50f, state.Stamina);
        Assert.Equal(50f, state.MaxStamina);
        Assert.False(state.IsInvincible);
        Assert.False(state.IsDodging);
        Assert.False(state.IsAttacking);
        Assert.Equal(0, state.ComboCount);
        Assert.Equal(0f, state.ComboTimer);
    }

    [Fact]
    public void TakeDamage_ReducesHealth()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);

        var result = CombatCalculator.TakeDamage(state, 30);

        Assert.Equal(70, result.State.Health);
        Assert.False(result.IsDead);
    }

    [Fact]
    public void TakeDamage_WhileInvincible_DoesNothing()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.IsInvincible = true;

        var result = CombatCalculator.TakeDamage(state, 30);

        Assert.Equal(100, result.State.Health);
        Assert.False(result.IsDead);
    }

    [Fact]
    public void TakeDamage_WhileDodging_DoesNothing()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.IsDodging = true;

        var result = CombatCalculator.TakeDamage(state, 30);

        Assert.Equal(100, result.State.Health);
        Assert.False(result.IsDead);
    }

    [Fact]
    public void TakeDamage_ToZero_TriggersIsDead()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);

        var result = CombatCalculator.TakeDamage(state, 100);

        Assert.Equal(0, result.State.Health);
        Assert.True(result.IsDead);
    }

    [Fact]
    public void TakeDamage_BeyondZero_ClampsToZero()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);

        var result = CombatCalculator.TakeDamage(state, 150);

        Assert.Equal(0, result.State.Health);
        Assert.True(result.IsDead);
    }

    [Fact]
    public void TryDodge_WithEnoughStamina_Succeeds()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);

        var result = CombatCalculator.TryDodge(state, 20f);

        Assert.True(result.CanDodge);
        Assert.True(result.State.IsDodging);
        Assert.Equal(30f, result.State.Stamina);
    }

    [Fact]
    public void TryDodge_WithoutEnoughStamina_Fails()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.Stamina = 10f;

        var result = CombatCalculator.TryDodge(state, 20f);

        Assert.False(result.CanDodge);
        Assert.False(result.State.IsDodging);
        Assert.Equal(10f, result.State.Stamina);
    }

    [Fact]
    public void TryDodge_WhileAttacking_Fails()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.IsAttacking = true;

        var result = CombatCalculator.TryDodge(state, 20f);

        Assert.False(result.CanDodge);
        Assert.False(result.State.IsDodging);
    }

    [Fact]
    public void TryDodge_WhileAlreadyDodging_Fails()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.IsDodging = true;

        var result = CombatCalculator.TryDodge(state, 20f);

        Assert.False(result.CanDodge);
    }

    [Fact]
    public void TryAttack_BasicDamage()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);

        var result = CombatCalculator.TryAttack(state, 25, 1.0f);

        Assert.Equal(25, result.Damage);
        Assert.False(result.IsComboFinisher);
        Assert.True(result.State.IsAttacking);
    }

    [Fact]
    public void TryAttack_WhileDodging_Fails()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.IsDodging = true;

        var result = CombatCalculator.TryAttack(state, 25, 1.0f);

        Assert.Equal(0, result.Damage);
        Assert.False(result.IsComboFinisher);
        Assert.False(result.State.IsAttacking);
    }

    [Fact]
    public void TryAttack_ComboIncrements()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);

        var result1 = CombatCalculator.TryAttack(state, 25, 1.0f);
        var result2 = CombatCalculator.TryAttack(result1.State, 25, 1.0f);

        Assert.Equal(2, result2.State.ComboCount);
        Assert.False(result2.IsComboFinisher);
    }

    [Fact]
    public void TryAttack_ComboFinisher_DoesTwoDamageAndResets()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);

        var result1 = CombatCalculator.TryAttack(state, 25, 1.0f);
        var result2 = CombatCalculator.TryAttack(result1.State, 25, 1.0f);
        var result3 = CombatCalculator.TryAttack(result2.State, 25, 1.0f);

        Assert.Equal(50, result3.Damage);
        Assert.True(result3.IsComboFinisher);
        Assert.Equal(0, result3.State.ComboCount);
        Assert.Equal(0f, result3.State.ComboTimer);
    }

    [Fact]
    public void EndDodge_ClearsDodgingAndInvincible()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.IsDodging = true;
        state.IsInvincible = true;

        var result = CombatCalculator.EndDodge(state);

        Assert.False(result.IsDodging);
        Assert.False(result.IsInvincible);
    }

    [Fact]
    public void EndAttack_ClearsAttacking()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.IsAttacking = true;

        var result = CombatCalculator.EndAttack(state);

        Assert.False(result.IsAttacking);
    }

    [Fact]
    public void RegenStamina_ClampsToMax()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.Stamina = 40f;

        var result = CombatCalculator.RegenStamina(state, 20f);

        Assert.Equal(50f, result.State.Stamina);
    }

    [Fact]
    public void RegenStamina_AddsAmount()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.Stamina = 20f;

        var result = CombatCalculator.RegenStamina(state, 10f);

        Assert.Equal(30f, result.State.Stamina);
    }

    [Fact]
    public void UpdateComboTimer_ResetsComboWhenExpired()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.ComboTimer = 0.5f;
        state.ComboCount = 2;

        var result = CombatCalculator.UpdateComboTimer(state, 1.0f);

        Assert.Equal(0f, result.ComboTimer);
        Assert.Equal(0, result.ComboCount);
    }

    [Fact]
    public void UpdateComboTimer_DecreasesTimer()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.ComboTimer = 1.0f;
        state.ComboCount = 1;

        var result = CombatCalculator.UpdateComboTimer(state, 0.3f);

        Assert.Equal(0.7f, result.ComboTimer, 0.001f);
        Assert.Equal(1, result.ComboCount);
    }

    [Fact]
    public void UpdateComboTimer_WhenAlreadyZero_DoesNothing()
    {
        var state = CombatCalculator.CreateInitial(100, 50f);
        state.ComboTimer = 0f;
        state.ComboCount = 0;

        var result = CombatCalculator.UpdateComboTimer(state, 1.0f);

        Assert.Equal(0f, result.ComboTimer);
        Assert.Equal(0, result.ComboCount);
    }
}
