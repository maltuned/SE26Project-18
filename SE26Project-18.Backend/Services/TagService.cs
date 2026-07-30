using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Infrastructure.Embedding;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Services.Recommendations;

namespace SE26Project_18.Backend.Services;

public class TagService : ITagService
{
    private readonly AppDbContext _db;
    private readonly MapperService _mapper;
    private readonly IEmbeddingSyncScheduler _embeddingSync;

    public TagService(AppDbContext db, MapperService mapper, IEmbeddingSyncScheduler embeddingSync)
    {
        _db = db;
        _mapper = mapper;
        _embeddingSync = embeddingSync;
    }

    public async Task<List<GameTagDto>> GetGameTagsAsync()
    {
        var tags = await _db.GameTags.ToListAsync();
        return tags.Select(_mapper.ToGameTagDto).ToList();
    }

    public async Task<List<RecruitmentTagDto>> GetRecruitmentTagsAsync()
    {
        var tags = await _db.RecruitmentTags.ToListAsync();
        return tags.Select(_mapper.ToRecruitmentTagDto).ToList();
    }

    public async Task<GameTagDto> CreateGameTagAsync(string name)
    {
        var tag = new GameTag(name);
        _db.GameTags.Add(tag);
        await _db.SaveChangesAsync();
        return _mapper.ToGameTagDto(tag);
    }

    public async Task<RecruitmentTagDto> CreateRecruitmentTagAsync(string name)
    {
        var tag = new RecruitmentTag(name);
        _db.RecruitmentTags.Add(tag);
        await _db.SaveChangesAsync();
        return _mapper.ToRecruitmentTagDto(tag);
    }

    public async Task<GameTagDto?> UpdateGameTagAsync(long id, string name)
    {
        var tag = await _db.GameTags.FindAsync(id);
        if (tag == null) return null;
        var gameIds = await _db.Games.Where(game => game.Tags.Any(item => item.Id == id))
            .Select(game => game.Id).ToListAsync();
        var recruitmentIds = await _db.Recruitments
            .Where(recruitment => recruitment.GameTags.Any(item => item.Id == id))
            .Select(recruitment => recruitment.Id).ToListAsync();
        tag.Name = name;
        _embeddingSync.Schedule(EmbeddingTarget.Game, gameIds);
        _embeddingSync.Schedule(EmbeddingTarget.User, await GetAffectedUserIdsAsync(recruitmentIds));
        await _db.SaveChangesAsync();
        return _mapper.ToGameTagDto(tag);
    }

    public async Task<RecruitmentTagDto?> UpdateRecruitmentTagAsync(long id, string name)
    {
        var tag = await _db.RecruitmentTags.FindAsync(id);
        if (tag == null) return null;
        var recruitmentIds = await _db.Recruitments
            .Where(recruitment => recruitment.RecruitmentTags.Any(item => item.Id == id))
            .Select(recruitment => recruitment.Id).ToListAsync();
        tag.Name = name;
        _embeddingSync.Schedule(EmbeddingTarget.Recruitment, recruitmentIds);
        _embeddingSync.Schedule(EmbeddingTarget.User, await GetAffectedUserIdsAsync(recruitmentIds));
        await _db.SaveChangesAsync();
        return _mapper.ToRecruitmentTagDto(tag);
    }

    public async Task<bool> DeleteGameTagAsync(long id)
    {
        var tag = await _db.GameTags.FindAsync(id);
        if (tag == null) return false;

        var games = await _db.Games.Include(g => g.Tags)
            .Where(g => g.Tags.Any(t => t.Id == id)).ToListAsync();
        foreach (var game in games)
        {
            game.Tags.Remove(tag);
        }

        var recruitments = await _db.Recruitments.Include(r => r.GameTags)
            .Where(r => r.GameTags.Any(t => t.Id == id)).ToListAsync();
        foreach (var rec in recruitments)
        {
            rec.GameTags.Remove(tag);
        }

        _db.GameTags.Remove(tag);
        _embeddingSync.Schedule(EmbeddingTarget.Game, games.Select(game => game.Id));
        _embeddingSync.Schedule(EmbeddingTarget.User,
            await GetAffectedUserIdsAsync(recruitments.Select(recruitment => recruitment.Id)));
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRecruitmentTagAsync(long id)
    {
        var tag = await _db.RecruitmentTags.FindAsync(id);
        if (tag == null) return false;

        var recruitments = await _db.Recruitments.Include(r => r.RecruitmentTags)
            .Where(r => r.RecruitmentTags.Any(t => t.Id == id)).ToListAsync();
        foreach (var rec in recruitments)
        {
            rec.RecruitmentTags.Remove(tag);
        }

        _db.RecruitmentTags.Remove(tag);
        _embeddingSync.Schedule(EmbeddingTarget.Recruitment,
            recruitments.Select(recruitment => recruitment.Id));
        _embeddingSync.Schedule(EmbeddingTarget.User,
            await GetAffectedUserIdsAsync(recruitments.Select(recruitment => recruitment.Id)));
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<IReadOnlyCollection<long>> GetAffectedUserIdsAsync(IEnumerable<long> recruitmentIds)
    {
        var ids = recruitmentIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        var publishers = _db.Recruitments.Where(item => ids.Contains(item.Id))
            .Select(item => item.PublisherId);
        var responders = _db.Responses.Where(item => ids.Contains(item.RecruitmentId))
            .Select(item => item.ResponserId);
        var viewers = _db.RecruitmentViews.Where(item => ids.Contains(item.RecruitmentId))
            .Select(item => item.UserId);
        return await publishers.Concat(responders).Concat(viewers).Distinct().ToListAsync();
    }
}
