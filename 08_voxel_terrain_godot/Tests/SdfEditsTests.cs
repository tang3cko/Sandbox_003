using VoxelTerrain.Core;
using Xunit;

namespace VoxelTerrain.Tests;

public class SdfEditsTests
{
    [Fact]
    public void FillFlatGround_PositiveBelowSolidNegativeAbove()
    {
        var field = new DensityField(8);
        SdfEdits.FillFlatGround(field, groundY: 4f);

        Assert.True(field.Get(0, 0, 0) > 0f);
        Assert.True(field.Get(0, 3, 0) > 0f);
        Assert.True(field.Get(0, 5, 0) < 0f);
        Assert.True(field.Get(0, 7, 0) < 0f);
    }

    [Fact]
    public void DigSphere_TurnsSolidIntoAirAtCenter()
    {
        var field = new DensityField(16);
        SdfEdits.FillFlatGround(field, groundY: 12f);

        Assert.True(field.IsSolid(8, 4, 8));

        int changed = SdfEdits.DigSphere(field, 8f, 4f, 8f, 2f);

        Assert.True(changed > 0);
        Assert.False(field.IsSolid(8, 4, 8));
    }

    [Fact]
    public void DigSphere_DoesNotAffectFarVoxels()
    {
        var field = new DensityField(16);
        SdfEdits.FillFlatGround(field, groundY: 12f);
        float before = field.Get(0, 0, 0);

        SdfEdits.DigSphere(field, 8f, 4f, 8f, 2f);

        Assert.Equal(before, field.Get(0, 0, 0));
    }

    [Fact]
    public void PlaceSphere_AddsSolidIntoAir()
    {
        var field = new DensityField(16);
        SdfEdits.FillFlatGround(field, groundY: 4f);

        Assert.False(field.IsSolid(8, 10, 8));

        int changed = SdfEdits.PlaceSphere(field, 8f, 10f, 8f, 2f);

        Assert.True(changed > 0);
        Assert.True(field.IsSolid(8, 10, 8));
    }

    [Fact]
    public void NegativeRadius_IsNoOp()
    {
        var field = new DensityField(8);
        SdfEdits.FillFlatGround(field, groundY: 4f);
        Assert.Equal(0, SdfEdits.DigSphere(field, 4f, 4f, 4f, -1f));
        Assert.Equal(0, SdfEdits.PlaceSphere(field, 4f, 4f, 4f, 0f));
    }
}
