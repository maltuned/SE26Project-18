using SE26Project_18.Backend.Data;
using System.Text.Json;
using SE26Project_18.Backend.Models.Dtos;
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
    public async Task GetUserProfile_ReturnsNotFound_WhenTargetDoesNotExist()
    {
        var db = CreateDb();
        var service = new UserService(db, _mapper);

        var (user, isPrivate) = await service.GetUserProfileAsync(1, 999);

        Assert.Null(user);
        Assert.False(isPrivate);
    }

    [Fact]
    public async Task GetUserProfile_HidesPrivateProfileFromAnotherUser_ButNotItsOwner()
    {
        var db = CreateDb();
        var target = new User("private-user", "pw")
        {
            Settings = new UserSettings { ProfileVisible = false },
        };
        db.Users.Add(target);
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);

        var hidden = await service.GetUserProfileAsync(target.Id + 1, target.Id);
        var ownProfile = await service.GetUserProfileAsync(target.Id, target.Id);

        Assert.Null(hidden.user);
        Assert.True(hidden.isPrivate);
        Assert.NotNull(ownProfile.user);
        Assert.Equal("private-user", ownProfile.user.Username);
        Assert.False(ownProfile.isPrivate);
    }

    [Fact]
    public async Task GetUserProfile_ReturnsPublicProfileToAnotherUser()
    {
        var db = CreateDb();
        var target = new User("public-user", "pw")
        {
            Settings = new UserSettings { ProfileVisible = true },
        };
        db.Users.Add(target);
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);

        var (user, isPrivate) = await service.GetUserProfileAsync(target.Id + 1, target.Id);

        Assert.NotNull(user);
        Assert.Equal("public-user", user.Username);
        Assert.NotNull(user.Settings);
        Assert.True(user.Settings.ProfileVisible);
        Assert.False(isPrivate);
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsers_WithSettings()
    {
        var db = CreateDb();
        db.Users.AddRange(
            new User("first", "pw")
            {
                Settings = new UserSettings { PushEnabled = false, ProfileVisible = true, DarkMode = true },
            },
            new User("second", "pw"));
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);

        var result = await service.GetUsersAsync();

        Assert.Equal(2, result.Count);
        var first = Assert.Single(result, user => user.Username == "first");
        Assert.NotNull(first.Settings);
        Assert.False(first.Settings.PushEnabled);
        Assert.True(first.Settings.DarkMode);
        Assert.Contains(result, user => user.Username == "second");
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

        Assert.NotNull(result);
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

        Assert.NotNull(result);
        Assert.Equal("女", result.Gender);
    }

    [Fact]
    public async Task UpdateUser_UpdatesAllFields_AndConvertsJsonElementAndNullValues()
    {
        var db = CreateDb();
        var user = new User("u", "pw")
        {
            Nickname = "Old",
            Avatar = "old.jpg",
            Signature = "Old signature",
            Gender = Gender.Other,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);
        using var json = JsonDocument.Parse("""{"nickname":"Json Nick","avatar":null,"gender":"男"}""");
        var data = new Dictionary<string, object>
        {
            ["nickname"] = json.RootElement.GetProperty("nickname"),
            ["avatar"] = json.RootElement.GetProperty("avatar"),
            ["signature"] = null!,
            ["gender"] = json.RootElement.GetProperty("gender"),
        };

        var result = await service.UpdateUserAsync(user.Id, data);

        Assert.NotNull(result);
        Assert.Equal("Json Nick", result.Nickname);
        Assert.Equal(string.Empty, result.Avatar);
        Assert.Equal(string.Empty, result.Signature);
        Assert.Equal("男", result.Gender);
        Assert.Equal(Gender.Male, user.Gender);
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
    public async Task UpdateUserStatus_UpdatesAndReturnsUser_WhenFound()
    {
        var db = CreateDb();
        var user = new User("u", "pw");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);

        var result = await service.UpdateUserStatusAsync(user.Id, UserStatus.Banned);

        Assert.NotNull(result);
        Assert.Equal("封禁", result.Status);
        Assert.Equal(UserStatus.Banned, user.Status);
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

    [Fact]
    public async Task GetUserSettings_CreatesDefaultSettings_WhenMissing()
    {
        var db = CreateDb();
        var user = new User("u", "pw");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);

        var result = await service.GetUserSettingsAsync(user.Id);

        Assert.NotNull(result);
        Assert.True(result.PushEnabled);
        Assert.True(result.ProfileVisible);
        Assert.False(result.DarkMode);
        var stored = Assert.Single(db.UserSettings);
        Assert.Equal(user.Id, stored.UserId);
    }

    [Fact]
    public async Task GetUserSettings_ReturnsExistingSettings_WithoutCreatingAnother()
    {
        var db = CreateDb();
        var user = new User("u", "pw")
        {
            Settings = new UserSettings
            {
                PushEnabled = false,
                ProfileVisible = false,
                DarkMode = true,
            },
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);

        var result = await service.GetUserSettingsAsync(user.Id);

        Assert.NotNull(result);
        Assert.False(result.PushEnabled);
        Assert.False(result.ProfileVisible);
        Assert.True(result.DarkMode);
        Assert.Single(db.UserSettings);
    }

    [Fact]
    public async Task UpdateUserSettings_CreatesSettings_WhenMissing()
    {
        var db = CreateDb();
        var user = new User("u", "pw");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);
        var requested = new UserSettingsDto
        {
            PushEnabled = false,
            ProfileVisible = false,
            DarkMode = true,
        };

        var result = await service.UpdateUserSettingsAsync(user.Id, requested);

        Assert.NotNull(result);
        Assert.False(result.PushEnabled);
        Assert.False(result.ProfileVisible);
        Assert.True(result.DarkMode);
        var stored = Assert.Single(db.UserSettings);
        Assert.Equal(user.Id, stored.UserId);
        Assert.False(stored.PushEnabled);
        Assert.False(stored.ProfileVisible);
        Assert.True(stored.DarkMode);
    }

    [Fact]
    public async Task UpdateUserSettings_UpdatesExistingSettings_WithoutCreatingAnother()
    {
        var db = CreateDb();
        var existing = new UserSettings
        {
            PushEnabled = true,
            ProfileVisible = true,
            DarkMode = false,
        };
        var user = new User("u", "pw") { Settings = existing };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new UserService(db, _mapper);
        var requested = new UserSettingsDto
        {
            PushEnabled = false,
            ProfileVisible = false,
            DarkMode = true,
        };

        var result = await service.UpdateUserSettingsAsync(user.Id, requested);

        Assert.NotNull(result);
        Assert.False(result.PushEnabled);
        Assert.False(result.ProfileVisible);
        Assert.True(result.DarkMode);
        Assert.Same(existing, Assert.Single(db.UserSettings));
    }
}
