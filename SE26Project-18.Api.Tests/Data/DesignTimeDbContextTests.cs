using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Entities;

namespace SE26Project_18.Api.Tests.Data;

public sealed class DesignTimeDbContextTests
{
    [Fact]
    public void SupportedServerVersion_IsMariaDb1011()
    {
        Assert.Equal(new Version(10, 11, 0), AppDbContextConfiguration.SupportedServerVersion.Version);
        MariaDbCompatibility.Validate(AppDbContextConfiguration.SupportedServerVersion);
    }

    [Fact]
    public async Task Factory_CreatesConfiguredCurrentModelWithoutConnecting()
    {
        await using var db = new AppDbContextFactory().CreateDbContext([]);
        var model = db.GetService<IDesignTimeModel>().Model;

        Assert.Equal("Pomelo.EntityFrameworkCore.MySql", db.Database.ProviderName);
        Assert.Equal("utf8mb4_unicode_ci", model.GetCollation());
        Assert.Equal(
            "utf8mb4",
            model.FindAnnotation("MySql:CharSet")?.Value?.ToString()
        );
        Assert.Equal(50, MaxLength<User>(db, nameof(User.Username)));
        Assert.Equal(100, MaxLength<User>(db, nameof(User.Nickname)));
        Assert.Equal(500, MaxLength<User>(db, nameof(User.Signature)));
        Assert.Equal(200, MaxLength<Game>(db, nameof(Game.Name)));
        Assert.Equal(4_000, MaxLength<Game>(db, nameof(Game.Description)));
        Assert.Equal(4_000, MaxLength<Message>(db, nameof(Message.Content)));
        Assert.Equal(44, MaxLength<RefreshToken>(db, nameof(RefreshToken.TokenHashed)));
    }

    [Fact]
    public async Task Model_HasNoChangesAfterLatestMigration()
    {
        await using var db = new AppDbContextFactory().CreateDbContext([]);

        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static int? MaxLength<TEntity>(AppDbContext db, string propertyName)
    {
        return db.Model.FindEntityType(typeof(TEntity))!.FindProperty(propertyName)!.GetMaxLength();
    }
}
