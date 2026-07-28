using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Recommendations;

namespace SE26Project_18.Api.Services.Recommendations;

internal sealed class UserPreferenceProfileBuilder : IUserPreferenceProfileBuilder
{
    private readonly AppDbContext _db;
    private readonly EmbeddingProfileBatchBuilder _batchBuilder;

    public UserPreferenceProfileBuilder(AppDbContext db, EmbeddingProfileBatchBuilder batchBuilder)
    {
        _db = db;
        _batchBuilder = batchBuilder;
    }

    public async Task<UserPreferenceProfile> BuildAsync(long userId, CancellationToken ct)
    {
        if (!await _db.Users.AnyAsync(user => user.Id == userId, ct))
            throw new NotFoundException("User not found.");

        var profile = (await _batchBuilder.BuildUsersAsync([userId], ct)).Single();
        return new UserPreferenceProfile(
            profile.OwnUserTagVector,
            profile.InterestedUserTagVector,
            profile.RecruitmentTagVector,
            profile.GameTagVector
        );
    }
}
