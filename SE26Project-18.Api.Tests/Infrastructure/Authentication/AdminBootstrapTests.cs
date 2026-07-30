using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Infrastructure.Authentication;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Tests.Infrastructure.Authentication;

public sealed class AdminBootstrapTests
{
    [Fact]
    public async Task InitializeAsync_CreatesFirstAdminWithBcryptPassword()
    {
        await using var db = CreateDbContext();
        var bootstrapper = CreateBootstrapper(db, true, "first-admin", "secure-password");

        await bootstrapper.InitializeAsync(CancellationToken.None);

        var admin = Assert.Single(await db.Users.ToListAsync());
        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("secure-password", admin.PasswordHashed));
        Assert.DoesNotContain("secure-password", admin.PasswordHashed);
    }

    [Fact]
    public async Task InitializeAsync_IsDisabledAndIdempotentOnceAdminExists()
    {
        await using var db = CreateDbContext();
        await CreateBootstrapper(db, false, string.Empty, string.Empty)
            .InitializeAsync(CancellationToken.None);
        Assert.Empty(await db.Users.ToListAsync());

        db.Users.Add(new User("existing-admin", "hash", UserRole.Admin));
        await db.SaveChangesAsync();
        await CreateBootstrapper(db, true, "another-admin", "secure-password")
            .InitializeAsync(CancellationToken.None);

        Assert.Single(await db.Users.ToListAsync());
    }

    [Fact]
    public async Task InitializeAsync_FailsWhenUsernameBelongsToNormalUser()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User("occupied", "hash", UserRole.User));
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateBootstrapper(db, true, "occupied", "secure-password")
                .InitializeAsync(CancellationToken.None)
        );

        Assert.Contains("non-admin user", exception.Message);
    }

    [Fact]
    public async Task InitializeAsync_RechecksAdminAfterAcquiringLock()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = CreateDbContext(databaseName);
        await using var competingDb = CreateDbContext(databaseName);
        var bootstrapper = CreateBootstrapper(
            db,
            true,
            "second-admin",
            "secure-password",
            new CallbackLock(async () =>
            {
                competingDb.Users.Add(new User("first-admin", "hash", UserRole.Admin));
                await competingDb.SaveChangesAsync();
            })
        );

        await bootstrapper.InitializeAsync(CancellationToken.None);

        Assert.Equal(1, await db.Users.CountAsync(user => user.Role == UserRole.Admin));
        Assert.False(await db.Users.AnyAsync(user => user.Username == "second-admin"));
    }

    [Fact]
    public async Task InitializeAsync_ConcurrentInstancesCreateOnlyOneAdmin()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var firstDb = CreateDbContext(databaseName);
        await using var secondDb = CreateDbContext(databaseName);
        var first = CreateBootstrapper(firstDb, true, "first-admin", "secure-password");
        var second = CreateBootstrapper(secondDb, true, "second-admin", "secure-password");

        await Task.WhenAll(
            first.InitializeAsync(CancellationToken.None),
            second.InitializeAsync(CancellationToken.None)
        );

        Assert.Equal(1, await firstDb.Users.CountAsync(user => user.Role == UserRole.Admin));
    }

    [Fact]
    public async Task InitializeAsync_AlwaysReleasesLockWhenRecheckFails()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User("occupied", "hash", UserRole.User));
        await db.SaveChangesAsync();
        var bootstrapLock = new CallbackLock();
        var bootstrapper = CreateBootstrapper(
            db,
            true,
            "occupied",
            "secure-password",
            bootstrapLock
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bootstrapper.InitializeAsync(CancellationToken.None)
        );

        Assert.True(bootstrapLock.Released);
    }

    [Theory]
    [InlineData("ab", "secure-password")]
    [InlineData("valid-name", "short")]
    public void Validator_UsesRegistrationCredentialRules(string username, string password)
    {
        var result = new AdminBootstrapOptionsValidator().Validate(
            null,
            new AdminBootstrapOptions
            {
                Enabled = true,
                Username = username,
                Password = password,
            }
        );

        Assert.True(result.Failed);
    }

    private static AdminBootstrapper CreateBootstrapper(
        AppDbContext db,
        bool enabled,
        string username,
        string password,
        IAdminBootstrapLock? bootstrapLock = null
    )
    {
        return new AdminBootstrapper(
            db,
            Options.Create(
                new AdminBootstrapOptions
                {
                    Enabled = enabled,
                    Username = username,
                    Password = password,
                }
            ),
            bootstrapLock ?? new AdminBootstrapLock(db)
        );
    }

    private static AppDbContext CreateDbContext(string? databaseName = null)
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .Options
        );
    }

    private sealed class CallbackLock(Func<Task>? onAcquire = null) : IAdminBootstrapLock
    {
        public bool Released { get; private set; }

        public async Task<IAsyncDisposable> AcquireAsync(CancellationToken ct)
        {
            if (onAcquire is not null)
            {
                await onAcquire();
            }

            return new Releaser(this);
        }

        private sealed class Releaser(CallbackLock owner) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                owner.Released = true;
                return ValueTask.CompletedTask;
            }
        }
    }
}
