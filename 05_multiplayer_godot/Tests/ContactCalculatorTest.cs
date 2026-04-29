using Xunit;

namespace SwarmSurvivor.Tests;

public class ContactCalculatorTest
{
    [Fact]
    public void TickCooldown_AboveZero_Decrements()
    {
        Assert.Equal(0.5f, ContactCalculator.TickCooldown(1.0f, 0.5f), 3);
    }

    [Fact]
    public void TickCooldown_ExactlyDt_ReturnsZero()
    {
        Assert.Equal(0f, ContactCalculator.TickCooldown(0.5f, 0.5f), 3);
    }

    [Fact]
    public void TickCooldown_BelowZero_ClampsToZero()
    {
        Assert.Equal(0f, ContactCalculator.TickCooldown(0.1f, 0.5f));
    }

    [Fact]
    public void TickCooldown_AtZero_StaysZero()
    {
        Assert.Equal(0f, ContactCalculator.TickCooldown(0f, 0.1f));
    }

    [Fact]
    public void IsCooldownReady_Positive_ReturnsFalse()
    {
        Assert.False(ContactCalculator.IsCooldownReady(0.5f));
    }

    [Fact]
    public void IsCooldownReady_Zero_ReturnsTrue()
    {
        Assert.True(ContactCalculator.IsCooldownReady(0f));
    }

    [Fact]
    public void IsCooldownReady_Negative_ReturnsTrue()
    {
        Assert.True(ContactCalculator.IsCooldownReady(-0.25f));
    }

    [Fact]
    public void IsWithinContactRadius_AtCenter_ReturnsTrue()
    {
        Assert.True(ContactCalculator.IsWithinContactRadius(5f, 5f, 5f, 5f, 1.5f));
    }

    [Fact]
    public void IsWithinContactRadius_AtBoundary_Inclusive()
    {
        Assert.True(ContactCalculator.IsWithinContactRadius(0f, 0f, 1.5f, 0f, 1.5f));
    }

    [Fact]
    public void IsWithinContactRadius_BeyondRadius_ReturnsFalse()
    {
        Assert.False(ContactCalculator.IsWithinContactRadius(0f, 0f, 2f, 0f, 1.5f));
    }

    [Fact]
    public void IsWithinContactRadius_LargeDistance_ReturnsFalse()
    {
        Assert.False(ContactCalculator.IsWithinContactRadius(0f, 0f, 100f, 100f, 1.5f));
    }

    [Fact]
    public void IsWithinContactRadius_ZeroRadius_OnlyExactPoint()
    {
        Assert.True(ContactCalculator.IsWithinContactRadius(3f, 4f, 3f, 4f, 0f));
        Assert.False(ContactCalculator.IsWithinContactRadius(3f, 4f, 3.0001f, 4f, 0f));
    }

    [Fact]
    public void IsWithinContactRadius_DiagonalWithinRadius_ReturnsTrue()
    {
        Assert.True(ContactCalculator.IsWithinContactRadius(0f, 0f, 1f, 1f, 1.5f));
    }

    [Fact]
    public void IsWithinContactRadius_DiagonalOutsideRadius_ReturnsFalse()
    {
        Assert.False(ContactCalculator.IsWithinContactRadius(0f, 0f, 1.5f, 1.5f, 1.5f));
    }
}
