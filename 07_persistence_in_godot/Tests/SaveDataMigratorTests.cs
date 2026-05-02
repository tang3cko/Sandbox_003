namespace Persistence.Tests;
using System.Collections.Generic;
using Xunit;

public class SaveDataMigratorTests
{
    [Fact]
    public void Migrate_CurrentVersion_UpToDate()
    {
        var input = new Dictionary<string, object>
        {
            ["save_version"] = SaveDataMigrator.CurrentVersion,
            ["money"] = 100,
        };
        var (result, status) = SaveDataMigrator.Migrate(input);
        Assert.Equal(SaveDataMigrator.MigrationStatus.UpToDate, status);
        Assert.NotNull(result);
        Assert.Equal(100, result["money"]);
    }

    [Fact]
    public void Migrate_FromV0_RenamesGoldToMoney()
    {
        var input = new Dictionary<string, object>
        {
            ["save_version"] = 0,
            ["gold"] = 250,
        };
        var (result, status) = SaveDataMigrator.Migrate(input);
        Assert.Equal(SaveDataMigrator.MigrationStatus.Migrated, status);
        Assert.NotNull(result);
        Assert.False(result.ContainsKey("gold"));
        Assert.Equal(250, result["money"]);
        Assert.Equal(SaveDataMigrator.CurrentVersion, result["save_version"]);
    }

    [Fact]
    public void Migrate_FromFuture_ReturnsFromFutureStatus()
    {
        var input = new Dictionary<string, object>
        {
            ["save_version"] = SaveDataMigrator.CurrentVersion + 100,
        };
        var (_, status) = SaveDataMigrator.Migrate(input);
        Assert.Equal(SaveDataMigrator.MigrationStatus.FromFuture, status);
    }

    [Fact]
    public void Migrate_MissingVersion_Invalid()
    {
        var input = new Dictionary<string, object> { ["money"] = 100 };
        var (result, status) = SaveDataMigrator.Migrate(input);
        Assert.Equal(SaveDataMigrator.MigrationStatus.Invalid, status);
        Assert.Null(result);
    }

    [Fact]
    public void Migrate_NonIntVersion_Invalid()
    {
        var input = new Dictionary<string, object> { ["save_version"] = "1" };
        var (_, status) = SaveDataMigrator.Migrate(input);
        Assert.Equal(SaveDataMigrator.MigrationStatus.Invalid, status);
    }

    [Fact]
    public void Migrate_NegativeVersion_Invalid()
    {
        var input = new Dictionary<string, object> { ["save_version"] = -1 };
        var (_, status) = SaveDataMigrator.Migrate(input);
        Assert.Equal(SaveDataMigrator.MigrationStatus.Invalid, status);
    }

    [Fact]
    public void Migrate_DoesNotMutateInput()
    {
        var input = new Dictionary<string, object>
        {
            ["save_version"] = 0,
            ["gold"] = 250,
        };
        SaveDataMigrator.Migrate(input);
        Assert.Equal(0, input["save_version"]);
        Assert.Equal(250, input["gold"]);
        Assert.False(input.ContainsKey("money"));
    }

    [Fact]
    public void Migrate_NullInput_Invalid()
    {
        var (result, status) = SaveDataMigrator.Migrate(null);
        Assert.Equal(SaveDataMigrator.MigrationStatus.Invalid, status);
        Assert.Null(result);
    }
}
