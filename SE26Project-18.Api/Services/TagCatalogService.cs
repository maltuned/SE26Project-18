using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

internal sealed class TagCatalogService : ITagCatalogService
{
    private readonly AppDbContext _db;

    public TagCatalogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GameTagResponse> CreateGameTagAsync(
        CreateTagRequest request,
        CancellationToken ct
    )
    {
        var name = ValidateName(request.Name);
        if (await _db.GameTags.AnyAsync(tag => tag.Name == name, ct))
        {
            throw new ConflictException("Game tag already exists.");
        }
        var tag = new GameTag(name);
        _db.GameTags.Add(tag);
        await _db.SaveChangesAsync(ct);
        return tag.ToResponse();
    }

    public async Task<UserTagResponse> CreateUserTagAsync(
        CreateTagRequest request,
        CancellationToken ct
    )
    {
        var name = ValidateName(request.Name);
        if (await _db.UserTags.AnyAsync(tag => tag.Name == name, ct))
        {
            throw new ConflictException("User tag already exists.");
        }
        var tag = new UserTag(name);
        _db.UserTags.Add(tag);
        await _db.SaveChangesAsync(ct);
        return tag.ToResponse();
    }

    public async Task<RecruitmentTagResponse> CreateRecruitmentTagAsync(
        CreateTagRequest request,
        CancellationToken ct
    )
    {
        var name = ValidateName(request.Name);
        if (await _db.RecruitmentTags.AnyAsync(tag => tag.Name == name, ct))
        {
            throw new ConflictException("Recruitment tag already exists.");
        }
        var tag = new RecruitmentTag(name);
        _db.RecruitmentTags.Add(tag);
        await _db.SaveChangesAsync(ct);
        return tag.ToResponse();
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Tag name is required.");
        }
        return name.Trim();
    }
}
