using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly MapperService _mapper;

    public UserService(AppDbContext db, MapperService mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<UserDto?> GetUserByIdAsync(long id)
    {
        var user = await _db.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == id);
        return user == null ? null : _mapper.ToUserDto(user);
    }

    public async Task<(UserDto? user, bool isPrivate)> GetUserProfileAsync(long requesterId, long targetId)
    {
        var user = await _db.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == targetId);

        if (user == null) return (null, false);

        if (requesterId != targetId && user.Settings?.ProfileVisible == false)
            return (null, true);

        return (_mapper.ToUserDto(user), false);
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var users = await _db.Users
            .Include(u => u.Settings)
            .ToListAsync();
        return users.Select(_mapper.ToUserDto).ToList();
    }

    public async Task<UserDto?> UpdateUserAsync(long id, Dictionary<string, object> data)
    {
        var user = await _db.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return null;

        if (data.TryGetValue("nickname", out var nick)) user.Nickname = GetStringValue(nick);
        if (data.TryGetValue("avatar", out var avatar)) user.Avatar = GetStringValue(avatar);
        if (data.TryGetValue("signature", out var sig)) user.Signature = GetStringValue(sig);
        if (data.TryGetValue("gender", out var gender)) user.Gender = GetStringValue(gender).ToGender();

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return _mapper.ToUserDto(user);
    }

    public async Task<List<UserDto>> SearchUsersAsync(string query)
    {
        if (long.TryParse(query, out var id))
        {
            var user = await _db.Users
                .Include(u => u.Settings)
                .Where(u => u.Id == id)
                .ToListAsync();
            return user.Select(_mapper.ToUserDto).ToList();
        }

        var users = await _db.Users
            .Include(u => u.Settings)
            .Where(u => u.Username.Contains(query) || u.Nickname.Contains(query))
            .ToListAsync();
        return users.Select(_mapper.ToUserDto).ToList();
    }

    public async Task<UserDto?> UpdateUserStatusAsync(long id, UserStatus status)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        user.Status = status;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return _mapper.ToUserDto(user);
    }

    public async Task<UserDto?> ClearUserProfileAsync(long id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        user.Nickname = "";
        user.Avatar = "";
        user.Signature = "";
        user.Gender = Gender.Other;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return _mapper.ToUserDto(user);
    }

    public async Task<UserSettingsDto?> GetUserSettingsAsync(long userId)
    {
        var settings = await _db.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);
        if (settings == null)
        {
            settings = new UserSettings
            {
                UserId = userId,
                PushEnabled = true,
                ProfileVisible = true,
                DarkMode = false,
            };
            _db.UserSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        return new UserSettingsDto
        {
            PushEnabled = settings.PushEnabled,
            ProfileVisible = settings.ProfileVisible,
            DarkMode = settings.DarkMode,
        };
    }

    public async Task<UserSettingsDto?> UpdateUserSettingsAsync(long userId, UserSettingsDto dto)
    {
        var settings = await _db.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);
        if (settings == null)
        {
            settings = new UserSettings
            {
                UserId = userId,
                PushEnabled = true,
                ProfileVisible = true,
                DarkMode = false,
            };
            _db.UserSettings.Add(settings);
        }

        settings.PushEnabled = dto.PushEnabled;
        settings.ProfileVisible = dto.ProfileVisible;
        settings.DarkMode = dto.DarkMode;
        await _db.SaveChangesAsync();

        return new UserSettingsDto
        {
            PushEnabled = settings.PushEnabled,
            ProfileVisible = settings.ProfileVisible,
            DarkMode = settings.DarkMode,
        };
    }

    private static string GetStringValue(object value)
    {
        if (value is System.Text.Json.JsonElement elem)
            return elem.GetString() ?? string.Empty;
        return value?.ToString() ?? string.Empty;
    }
}