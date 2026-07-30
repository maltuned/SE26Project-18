using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class ResponseServiceTests
{
    private AppDbContext CreateDb() => TestDbContextFactory.Create();
    private readonly MapperService _mapper = new();

    private (User, User, Recruitment) SeedData(AppDbContext db)
    {
        var publisher = new User("pub", "pw") { Nickname = "Publisher" };
        var responser = new User("resp", "pw") { Nickname = "Responser" };
        db.Users.AddRange(publisher, responser);
        db.Games.Add(new Game("TestGame"));
        db.SaveChanges();
        var recruitment = new Recruitment("Title", DateTime.UtcNow.AddDays(30), 5)
        {
            PublisherId = publisher.Id, GameId = 1, Publisher = publisher
        };
        db.Recruitments.Add(recruitment);
        db.SaveChanges();
        return (publisher, responser, recruitment);
    }

    [Fact]
    public async Task CreateResponse_Throws_WhenRecruitmentNotFound()
    {
        var db = CreateDb();
        var u = new User("user", "pw");
        db.Users.Add(u);
        await db.SaveChangesAsync();
        var service = new ResponseService(db, _mapper);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateResponseAsync(999, u.Id));
    }

    [Fact]
    public async Task CreateResponse_Throws_WhenResponserNotFound()
    {
        var db = CreateDb();
        var (publisher, _, recruitment) = SeedData(db);
        var service = new ResponseService(db, _mapper);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateResponseAsync(recruitment.Id, 999));
    }

    [Fact]
    public async Task CreateResponse_Throws_WhenDuplicate()
    {
        var db = CreateDb();
        var (publisher, responser, recruitment) = SeedData(db);
        db.Responses.Add(new Models.Entities.Response
        {
            RecruitmentId = recruitment.Id, ResponserId = responser.Id,
            ResponseStatus = ResponseStatus.Responded
        });
        await db.SaveChangesAsync();
        var service = new ResponseService(db, _mapper);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateResponseAsync(recruitment.Id, responser.Id));
    }

    [Fact]
    public async Task CreateResponse_Succeeds_WithValidData()
    {
        var db = CreateDb();
        var (publisher, responser, recruitment) = SeedData(db);

        var userService = new ResponseService(db, _mapper);
        var result = await userService.CreateResponseAsync(recruitment.Id, responser.Id);

        Assert.NotNull(result);
        Assert.Equal(responser.Id, result.ResponserId);
    }

    [Fact]
    public async Task DeleteResponse_ReturnsFalse_WhenNotFound()
    {
        var db = CreateDb();
        var service = new ResponseService(db, _mapper);

        var result = await service.DeleteResponseAsync(999, "reason");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteResponse_CreatesChat_WhenNoChatExists()
    {
        var db = CreateDb();
        var (publisher, responser, recruitment) = SeedData(db);
        var response = new Models.Entities.Response
        {
            RecruitmentId = recruitment.Id, ResponserId = responser.Id,
            ResponseStatus = ResponseStatus.Responded
        };
        db.Responses.Add(response);
        await db.SaveChangesAsync();
        var service = new ResponseService(db, _mapper);

        var result = await service.DeleteResponseAsync(response.Id, "Not interested");

        Assert.True(result);
        Assert.NotEmpty(db.Chats);
        Assert.NotEmpty(db.Messages);
        var msg = db.Messages.First();
        Assert.Contains("Not interested", msg.Content);
    }

    [Fact]
    public async Task UpdateResponseStatus_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var service = new ResponseService(db, _mapper);

        var result = await service.UpdateResponseStatusAsync(999, "accepted");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetResponsesByRecruitment_ReturnsResponses()
    {
        var db = CreateDb();
        var (publisher, responser, recruitment) = SeedData(db);
        db.Responses.Add(new Models.Entities.Response
        {
            RecruitmentId = recruitment.Id, ResponserId = responser.Id,
            ResponseStatus = ResponseStatus.Responded, Responser = responser
        });
        await db.SaveChangesAsync();
        var service = new ResponseService(db, _mapper);

        var result = await service.GetResponsesByRecruitmentAsync(recruitment.Id);

        Assert.Single(result);
    }
}
