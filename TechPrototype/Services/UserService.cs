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

    public async Task<UserDto?> LoginAsync(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHashed))
            return null;
        return _mapper.ToUserDto(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(long id)
    {
        var user = await _db.Users.FindAsync(id);
        return user == null ? null : _mapper.ToUserDto(user);
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var users = await _db.Users.ToListAsync();
        return users.Select(_mapper.ToUserDto).ToList();
    }

    public async Task<UserDto> RegisterAsync(string username, string password)
    {
        var exists = await _db.Users.AnyAsync(u => u.Username == username);
        if (exists)
            throw new InvalidOperationException("用户名已存在");

        var hashed = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User(username, hashed)
        {
            Nickname = username,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return _mapper.ToUserDto(user);
    }

    public async Task<UserDto?> UpdateUserAsync(long id, Dictionary<string, object> data)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        if (data.TryGetValue("nickname", out var nick)) user.Nickname = GetStringValue(nick);
        if (data.TryGetValue("avatar", out var avatar)) user.Avatar = GetStringValue(avatar);
        if (data.TryGetValue("signature", out var sig)) user.Signature = GetStringValue(sig);
        if (data.TryGetValue("gender", out var gender)) user.Gender = GetStringValue(gender).ToGender();

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return _mapper.ToUserDto(user);
    }

    private static string GetStringValue(object value)
    {
        if (value is System.Text.Json.JsonElement elem)
            return elem.GetString() ?? string.Empty;
        return value?.ToString() ?? string.Empty;
    }
}