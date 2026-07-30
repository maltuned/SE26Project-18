using SE26Project_18.Backend.Data;
using System.Text.Json;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class UserServiceTests
{
    private AppDbContext CreateDb() => TestDbContextFactory.Create();
    private readonly MapperService _mapper = new();

    [Fact]
    public async Task GetUserById_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var service = new UserService(db, _mapper);

        var result = await service.GetUserByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserById_ReturnsUser_WhenFound()
    {
        var db = CreateDb();
        db.Users.Add(new User("u", "pw") { Nickname = "Nick" });
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);

        var result = await service.GetUserByIdAsync(db.Users.First().Id);

        Assert.NotNull(result);
        Assert.Equal("u", result.Username);
        Assert.Equal("Nick", result.Nickname);
    }

    [Fact]
    public async Task UpdateUser_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var service = new UserService(db, _mapper);

        var result = await service.UpdateUserAsync(999, new Dictionary<string, object>());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUser_UpdatesNickname()
    {
        var db = CreateDb();
        db.Users.Add(new User("u", "pw"));
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);
        var data = new Dictionary<string, object> { { "nickname", "NewNick" } };

        var result = await service.UpdateUserAsync(db.Users.First().Id, data);

        Assert.NotNull(result);
        Assert.Equal("NewNick", result.Nickname);
    }

    [Fact]
    public async Task UpdateUser_UpdatesAvatar()
    {
        var db = CreateDb();
        db.Users.Add(new User("u", "pw"));
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);
        var data = new Dictionary<string, object> { { "avatar", "new-avatar.jpg" } };

        var result = await service.UpdateUserAsync(db.Users.First().Id, data);

        Assert.Equal("new-avatar.jpg", result.Avatar);
    }

    [Fact]
    public async Task UpdateUser_UpdatesGender_WithString()
    {
        var db = CreateDb();
        db.Users.Add(new User("u", "pw"));
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);
        var data = new Dictionary<string, object> { { "gender", "女" } };

        var result = await service.UpdateUserAsync(db.Users.First().Id, data);

        Assert.Equal("女", result.Gender);
    }

    [Fact]
    public async Task SearchUsers_ById_WhenNumericQuery()
    {
        var db = CreateDb();
        db.Users.Add(new User("user1", "pw"));
        db.Users.Add(new User("user2", "pw"));
        await db.SaveChangesAsync();
        var firstId = db.Users.First().Id;
        var service = new UserService(db, _mapper);

        var result = await service.SearchUsersAsync(firstId.ToString());

        Assert.Single(result);
    }

    [Fact]
    public async Task SearchUsers_ByName_WhenTextQuery()
    {
        var db = CreateDb();
        db.Users.Add(new User("john", "pw"));
        db.Users.Add(new User("jane", "pw") { Nickname = "Johnny" });
        db.Users.Add(new User("bob", "pw"));
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);

        var result = await service.SearchUsersAsync("john");

        Assert.Single(result);
    }

    [Fact]
    public async Task UpdateUserStatus_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var service = new UserService(db, _mapper);

        var result = await service.UpdateUserStatusAsync(999, UserStatus.Banned);

        Assert.Null(result);
    }

    [Fact]
    public async Task ClearUserProfile_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var service = new UserService(db, _mapper);

        var result = await service.ClearUserProfileAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task ClearUserProfile_ClearsFields()
    {
        var db = CreateDb();
        db.Users.Add(new User("u", "pw") { Nickname = "OldNick", Avatar = "old.jpg", Signature = "Old sig" });
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);

        var result = await service.ClearUserProfileAsync(db.Users.First().Id);

        Assert.NotNull(result);
        Assert.Equal("", result.Nickname);
        Assert.Equal("", result.Avatar);
        Assert.Equal("", result.Signature);
    }
}
