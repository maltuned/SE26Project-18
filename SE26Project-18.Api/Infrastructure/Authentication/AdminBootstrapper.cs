using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Infrastructure.Authentication;

internal sealed class AdminBootstrapper
{
    private readonly AppDbContext _db;

    private readonly AdminBootstrapOptions _options;

    private readonly IAdminBootstrapLock _bootstrapLock;

    public AdminBootstrapper(
        AppDbContext db,
        IOptions<AdminBootstrapOptions> options,
        IAdminBootstrapLock bootstrapLock
    )
    {
        _db = db;
        _options = options.Value;
        _bootstrapLock = bootstrapLock;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            return;
        }

        await using var bootstrapLock = await _bootstrapLock.AcquireAsync(ct);
        _db.ChangeTracker.Clear();
        if (await _db.Users.AnyAsync(user => user.Role == UserRole.Admin, ct))
        {
            return;
        }

        if (await _db.Users.AnyAsync(user => user.Username == _options.Username, ct))
        {
            throw new InvalidOperationException(
                "Admin bootstrap username is already assigned to a non-admin user."
            );
        }

        var passwordHashed = BCrypt.Net.BCrypt.HashPassword(_options.Password);
        _db.Users.Add(new User(_options.Username, passwordHashed, UserRole.Admin));
        await _db.SaveChangesAsync(ct);
    }
}
