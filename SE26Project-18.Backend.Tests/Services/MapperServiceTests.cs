using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class MapperServiceTests
{
    private readonly MapperService _mapper = new();

    private User CreateUser(long id = 1, string username = "testuser", string nickname = "Test")
    {
        var user = new User(username, "hashedpw");
        typeof(User).GetProperty("Id")!.SetValue(user, id);
        user.Nickname = nickname;
        user.Avatar = "avatar.png";
        user.Signature = "Hello world";
        user.Gender = Gender.Male;
        user.Status = UserStatus.Normal;
        return user;
    }

    private Game CreateGame(long id = 1, string name = "TestGame")
    {
        var game = new Game(name);
        typeof(Game).GetProperty("Id")!.SetValue(game, id);
        game.NameEn = "TestGameEN";
        game.Aliases = "TG,TGame";
        game.Company = "TestCorp";
        game.Description = "A test game";
        game.Cover = "cover.jpg";
        game.Icon = "icon.png";
        game.Tags = new List<GameTag> { new GameTag("RPG"), new GameTag("Action") };
        return game;
    }

    private GameTag CreateGameTag(long id = 1, string name = "RPG")
    {
        var tag = new GameTag(name);
        typeof(GameTag).GetProperty("Id")!.SetValue(tag, id);
        return tag;
    }

    private RecruitmentTag CreateRecruitmentTag(long id = 1, string name = "Casual")
    {
        var tag = new RecruitmentTag(name);
        typeof(RecruitmentTag).GetProperty("Id")!.SetValue(tag, id);
        return tag;
    }

    private Recruitment CreateRecruitment(long id = 1)
    {
        var publisher = CreateUser(1, "pub", "Publisher");
        var game = CreateGame(1, "Game");
        var recruitment = new Recruitment("Looking for team", DateTime.UtcNow.AddDays(30), 5)
        {
            PublisherId = 1,
            GameId = 1,
            Description = "Description here",
            Publisher = publisher,
            Game = game,
        };
        typeof(Recruitment).GetProperty("Id")!.SetValue(recruitment, id);
        recruitment.RecruitmentTags = new List<RecruitmentTag> { CreateRecruitmentTag(1, "Casual") };
        recruitment.GameTags = new List<GameTag> { CreateGameTag(1, "RPG") };
        recruitment.Publisher = publisher;
        recruitment.Game = game;
        return recruitment;
    }

    private Chat CreateChat(long id = 1)
    {
        var recruiter = CreateUser(1, "recruiter", "Recruiter");
        var responser = CreateUser(2, "responser", "Responser");
        var chat = new Chat
        {
            RecruitmentId = 1,
            RecruiterId = 1,
            ResponserId = 2,
            Recruiter = recruiter,
            Responser = responser,
            ChatStatus = ChatStatus.Open,
        };
        typeof(Chat).GetProperty("Id")!.SetValue(chat, id);
        var message = new Message("Hello") { SenderId = 1, ReceiverId = 2, Sender = recruiter, Receiver = responser };
        typeof(Message).GetProperty("Id")!.SetValue(message, 1);
        chat.Messages = new List<Message> { message };
        chat.NewMessageAt = DateTime.UtcNow;
        return chat;
    }

    // ==================== User Mapping Tests ====================

    [Fact]
    public void ToUserDto_MapsAllFields()
    {
        var user = CreateUser();

        var dto = _mapper.ToUserDto(user);

        Assert.Equal(1L, dto.Id);
        Assert.Equal(1L, dto.Uid);
        Assert.Equal("testuser", dto.Username);
        Assert.Equal("Test", dto.Nickname);
        Assert.Equal("avatar.png", dto.Avatar);
        Assert.Equal("Hello world", dto.Signature);
        Assert.Equal("男", dto.Gender);
        Assert.Equal("正常", dto.Status);
        Assert.NotNull(dto.CreatedAt);
        Assert.NotNull(dto.UpdatedAt);
    }

    [Fact]
    public void ToUserBriefDto_MapsIdNicknameUsernameAvatar()
    {
        var user = CreateUser(5, "john", "JohnDoe");

        var dto = _mapper.ToUserBriefDto(user);

        Assert.Equal(5L, dto.Id);
        Assert.Equal("JohnDoe", dto.Nickname);
        Assert.Equal("john", dto.Username);
        Assert.Equal("avatar.png", dto.Avatar);
    }

    // ==================== Game Mapping Tests ====================

    [Fact]
    public void ToGameDto_MapsAllFields()
    {
        var game = CreateGame();

        var dto = _mapper.ToGameDto(game);

        Assert.Equal(1L, dto.Id);
        Assert.Equal("TestGame", dto.Name);
        Assert.Equal("TestGameEN", dto.NameEn);
        Assert.Equal("TG,TGame", dto.Aliases);
        Assert.Equal("TestCorp", dto.Company);
        Assert.Equal("A test game", dto.Description);
        Assert.Equal("cover.jpg", dto.Cover);
        Assert.Equal("icon.png", dto.Icon);
        Assert.Equal(2, dto.TagsId.Length);
        Assert.Equal(2, dto.Tags.Length);
    }

    [Fact]
    public void ToGameDto_UsesExplicitTagsId_WhenProvided()
    {
        var game = CreateGame();
        var explicitTags = new long[] { 10, 20, 30 };

        var dto = _mapper.ToGameDto(game, explicitTags);

        Assert.Equal(new long[] { 10, 20, 30 }, dto.TagsId);
    }

    [Fact]
    public void ToGameDto_UsesGameTags_WhenNoExplicitTags()
    {
        var game = CreateGame();
        // Set tag IDs on the game's tags
        typeof(GameTag).GetProperty("Id")!.SetValue(game.Tags.ElementAt(0), 100L);
        typeof(GameTag).GetProperty("Id")!.SetValue(game.Tags.ElementAt(1), 200L);

        var dto = _mapper.ToGameDto(game);

        Assert.Contains(100L, dto.TagsId);
        Assert.Contains(200L, dto.TagsId);
    }

    [Fact]
    public void ToGameBriefDto_MapsBasicFields()
    {
        var game = CreateGame(42, "EldenRing");

        var dto = _mapper.ToGameBriefDto(game);

        Assert.Equal(42L, dto.Id);
        Assert.Equal("EldenRing", dto.Name);
        Assert.Equal("TestGameEN", dto.NameEn);
        Assert.Equal("cover.jpg", dto.Cover);
        Assert.Equal("icon.png", dto.Icon);
    }

    // ==================== Tag Mapping Tests ====================

    [Fact]
    public void ToGameTagDto_MapsIdAndName()
    {
        var tag = CreateGameTag(7, "FPS");

        var dto = _mapper.ToGameTagDto(tag);

        Assert.Equal(7L, dto.Id);
        Assert.Equal("FPS", dto.Name);
    }

    [Fact]
    public void ToRecruitmentTagDto_MapsIdAndName()
    {
        var tag = CreateRecruitmentTag(3, "Ranked");

        var dto = _mapper.ToRecruitmentTagDto(tag);

        Assert.Equal(3L, dto.Id);
        Assert.Equal("Ranked", dto.Name);
    }

    // ==================== Recruitment Mapping Tests ====================

    [Fact]
    public void ToRecruitmentDto_MapsAllFields()
    {
        var recruitment = CreateRecruitment();

        var dto = _mapper.ToRecruitmentDto(recruitment);

        Assert.Equal(1L, dto.Id);
        Assert.Equal(1L, dto.PublisherId);
        Assert.Equal(1L, dto.GameId);
        Assert.Equal("Looking for team", dto.Title);
        Assert.Equal("Description here", dto.Description);
        Assert.Equal("招募中", dto.Status);
        Assert.Single(dto.TagsId);
        Assert.Equal(5, dto.MaxParticipants);
        Assert.Equal(0, dto.CurrentParticipants);
    }

    [Fact]
    public void ToRecruitmentBriefDto_MapsIdTitleAndGame()
    {
        var recruitment = CreateRecruitment();

        var dto = _mapper.ToRecruitmentBriefDto(recruitment);

        Assert.Equal(1L, dto.Id);
        Assert.Equal("Looking for team", dto.Title);
        Assert.NotNull(dto.Game);
        Assert.Equal("Game", dto.Game.Name);
    }

    [Fact]
    public void ToRecruitmentDetailDto_MapsAllIncludingNestedDtos()
    {
        var recruitment = CreateRecruitment();

        var dto = _mapper.ToRecruitmentDetailDto(recruitment);

        Assert.Equal(1L, dto.Id);
        Assert.NotNull(dto.Publisher);
        Assert.NotNull(dto.Game);
        Assert.NotNull(dto.GameTags);
        Assert.NotNull(dto.RecruitmentTags);
        Assert.Equal("Publisher", dto.Publisher.Nickname);
        Assert.Single(dto.RecruitmentTags);
    }

    // ==================== Response Mapping Tests ====================

    [Fact]
    public void ToResponseDto_MapsAllFields()
    {
        var response = new Models.Entities.Response
        {
            RecruitmentId = 1,
            ResponserId = 2,
            ResponseStatus = ResponseStatus.Responded,
            Responser = CreateUser(2, "resp", "Responder"),
        };
        typeof(Models.Entities.Response).GetProperty("Id")!.SetValue(response, 10);

        var dto = _mapper.ToResponseDto(response);

        Assert.Equal(10L, dto.Id);
        Assert.Equal(1L, dto.RecruitmentId);
        Assert.Equal(2L, dto.ResponserId);
        Assert.Equal("已回应", dto.ResponseStatus);
        Assert.NotNull(dto.Responser);
        Assert.Equal("Responder", dto.Responser.Nickname);
    }

    // ==================== Message Mapping Tests ====================

    [Fact]
    public void ToMessageDto_MapsAllFields()
    {
        var sender = CreateUser(1, "sender", "Sender");
        var receiver = CreateUser(2, "receiver", "Receiver");
        var message = new Message("Test message") { ChatId = 5, SenderId = 1, ReceiverId = 2, Sender = sender, Receiver = receiver };
        typeof(Message).GetProperty("Id")!.SetValue(message, 100);

        var dto = _mapper.ToMessageDto(message);

        Assert.Equal(100L, dto.Id);
        Assert.Equal(5L, dto.ChatId);
        Assert.Equal(1L, dto.SenderId);
        Assert.Equal(2L, dto.ReceiverId);
        Assert.Equal("Test message", dto.Content);
        Assert.NotNull(dto.Sender);
        Assert.NotNull(dto.Receiver);
        Assert.Equal("Sender", dto.Sender.Nickname);
        Assert.Equal("Receiver", dto.Receiver.Nickname);
    }

    // ==================== ChatBriefDto Tests ====================

    [Fact]
    public void ToChatBriefDto_IdentifiesOtherUser_CorrectlyWhenCurrentIsRecruiter()
    {
        var chat = CreateChat();

        var dto = _mapper.ToChatBriefDto(chat, currentUserId: 1); // current is recruiter

        Assert.Equal("Responser", dto.OtherUserName);
        Assert.Equal("avatar.png", dto.OtherUserAvatar);
    }

    [Fact]
    public void ToChatBriefDto_IdentifiesOtherUser_CorrectlyWhenCurrentIsResponser()
    {
        var chat = CreateChat();

        var dto = _mapper.ToChatBriefDto(chat, currentUserId: 2); // current is responser

        Assert.Equal("Recruiter", dto.OtherUserName);
    }

    [Fact]
    public void ToChatBriefDto_ShowsLastMessageContent()
    {
        var chat = CreateChat();

        var dto = _mapper.ToChatBriefDto(chat, 1);

        Assert.Equal("Hello", dto.LastMessageContent);
        Assert.NotNull(dto.LastMessageAt);
    }

    [Fact]
    public void ToChatBriefDto_EmptyContent_WhenNoMessages()
    {
        var chat = CreateChat();
        chat.Messages = new List<Message>();

        var dto = _mapper.ToChatBriefDto(chat, 1);

        Assert.Equal("", dto.LastMessageContent);
        Assert.NotNull(dto.LastMessageAt); // falls back to chat's CreatedAt
    }

    // ==================== ChatDto Tests ====================

    [Fact]
    public void ToChatDto_MapsOtherUserCorrectly()
    {
        var chat = CreateChat();

        var dto = _mapper.ToChatDto(chat, 1); // current is recruiter

        Assert.NotNull(dto.OtherUser);
        Assert.Equal("Responser", dto.OtherUser.Nickname);
    }

    [Fact]
    public void ToChatDto_TracksSentMessageStatus()
    {
        var chat = CreateChat();
        // Only recruiter has sent a message
        chat.Messages = new List<Message>
        {
            new Message("Hi") { SenderId = 1, ReceiverId = 2, Sender = chat.Recruiter, Receiver = chat.Responser }
        };

        var dto = _mapper.ToChatDto(chat, 1);

        Assert.NotNull(dto.Users);
        Assert.Equal(2, dto.Users.Length);
        var recruiterUser = dto.Users.First(u => u.UserId == 1);
        var responserUser = dto.Users.First(u => u.UserId == 2);
        Assert.True(recruiterUser.SentMessage);
        Assert.False(responserUser.SentMessage);
    }

    [Fact]
    public void ToChatDto_HandlesNullRecruitment()
    {
        var chat = CreateChat();
        chat.Recruitment = null!;

        var dto = _mapper.ToChatDto(chat, 1);

        Assert.Equal("", dto.RecruitmentTitle);
        Assert.NotNull(dto.Recruitment);
        Assert.Equal(1L, dto.Recruitment.Id);
        Assert.Equal("", dto.Recruitment.Title);
    }

    [Fact]
    public void ToChatDto_HandlesNullMessagesCollection()
    {
        var chat = CreateChat();
        chat.Messages = null!;

        var dto = _mapper.ToChatDto(chat, 1);

        Assert.Null(dto.LastMessage);
        Assert.NotNull(dto.Users);
        Assert.False(dto.Users[0].SentMessage);
        Assert.False(dto.Users[1].SentMessage);
    }

    [Fact]
    public void ToChatDto_HandlesNullOtherUser()
    {
        var chat = CreateChat();
        chat.Recruiter = null!;
        chat.Responser = null!;

        var dto = _mapper.ToChatDto(chat, 1);

        Assert.NotNull(dto.OtherUser);
        Assert.Equal(0L, dto.OtherUser.Id);
    }
}
