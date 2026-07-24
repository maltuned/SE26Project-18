using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;

namespace SE26Project_18.Backend.Services;

public class TagService : ITagService
{
    private readonly AppDbContext _db;
    private readonly MapperService _mapper;

    public TagService(AppDbContext db, MapperService mapper)
    {
        _db = db;
        _mapper = mapper;
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
        tag.Name = name;
        await _db.SaveChangesAsync();
        return _mapper.ToGameTagDto(tag);
    }

    public async Task<RecruitmentTagDto?> UpdateRecruitmentTagAsync(long id, string name)
    {
        var tag = await _db.RecruitmentTags.FindAsync(id);
        if (tag == null) return null;
        tag.Name = name;
        await _db.SaveChangesAsync();
        return _mapper.ToRecruitmentTagDto(tag);
    }

    public async Task<bool> DeleteGameTagAsync(long id)
    {
        var tag = await _db.GameTags.FindAsync(id);
        if (tag == null) return false;
        _db.GameTags.Remove(tag);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRecruitmentTagAsync(long id)
    {
        var tag = await _db.RecruitmentTags.FindAsync(id);
        if (tag == null) return false;
        _db.RecruitmentTags.Remove(tag);
        await _db.SaveChangesAsync();
        return true;
    }
}
