using SE26Project_18.Backend.Models;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;

namespace SE26Project_18.Backend.Services;

public class MapperService
{
    public UserDto ToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Uid = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Avatar = user.Avatar,
            Signature = user.Signature,
            Gender = user.Gender.ToDtoString(),
            Status = user.Status.ToDtoString(),
            CreatedAt = user.CreatedAt.ToString("o"),
            UpdatedAt = user.UpdatedAt.ToString("o"),
        };
    }

    public UserBriefDto ToUserBriefDto(User user)
    {
        return new UserBriefDto
        {
            Id = user.Id,
            Nickname = user.Nickname,
            Username = user.Username,
            Avatar = user.Avatar,
        };
    }

    public GameDto ToGameDto(Game game, long[]? tagsId = null)
    {
        return new GameDto
        {
            Id = game.Id,
            Name = game.Name,
            NameEn = game.NameEn,
            Aliases = game.Aliases,
            Company = game.Company,
            Description = game.Description,
            Cover = game.Cover,
            Icon = game.Icon,
            TagsId = tagsId ?? game.Tags.Select(t => t.Id).ToArray(),
            Tags = game.Tags.Select(t => new GameTagDto { Id = t.Id, Name = t.Name }).ToArray(),
            CreatedAt = game.CreatedAt.ToString("o"),
            UpdatedAt = game.UpdatedAt.ToString("o"),
        };
    }

    public GameBriefDto ToGameBriefDto(Game game)
    {
        return new GameBriefDto
        {
            Id = game.Id,
            Name = game.Name,
            NameEn = game.NameEn,
            Cover = game.Cover,
            Icon = game.Icon,
        };
    }

    public GameTagDto ToGameTagDto(GameTag tag)
    {
        return new GameTagDto { Id = tag.Id, Name = tag.Name };
    }

    public RecruitmentTagDto ToRecruitmentTagDto(RecruitmentTag tag)
    {
        return new RecruitmentTagDto { Id = tag.Id, Name = tag.Name };
    }

    public RecruitmentDto ToRecruitmentDto(Recruitment r)
    {
        return new RecruitmentDto
        {
            Id = r.Id,
            PublisherId = r.PublisherId,
            GameId = r.GameId,
            Title = r.Title,
            Description = r.Description,
            Status = r.Status.ToDtoString(),
            TagsId = r.RecruitmentTags.Select(t => t.Id).ToArray(),
            CreatedAt = r.CreatedAt.ToString("o"),
            UpdatedAt = r.UpdatedAt.ToString("o"),
            ExpiredAt = r.ExpiredAt.ToString("o"),
            MaxParticipants = r.MaxParticipants,
            CurrentParticipants = r.CurrentParticipants,
        };
    }

    public RecruitmentBriefDto ToRecruitmentBriefDto(Recruitment r)
    {
        return new RecruitmentBriefDto
        {
            Id = r.Id,
            Title = r.Title,
            Game = ToGameBriefDto(r.Game),
        };
    }

    public RecruitmentDetailDto ToRecruitmentDetailDto(Recruitment r)
    {
        return new RecruitmentDetailDto
        {
            Id = r.Id,
            PublisherId = r.PublisherId,
            GameId = r.GameId,
            Title = r.Title,
            Description = r.Description,
            Status = r.Status.ToDtoString(),
            TagsId = r.RecruitmentTags.Select(t => t.Id).ToArray(),
            CreatedAt = r.CreatedAt.ToString("o"),
            UpdatedAt = r.UpdatedAt.ToString("o"),
            ExpiredAt = r.ExpiredAt.ToString("o"),
            MaxParticipants = r.MaxParticipants,
            CurrentParticipants = r.CurrentParticipants,
            Publisher = ToUserBriefDto(r.Publisher),
            Game = ToGameBriefDto(r.Game),
            GameTags = r.GameTags.Select(ToGameTagDto).ToArray(),
            RecruitmentTags = r.RecruitmentTags.Select(ToRecruitmentTagDto).ToArray(),
        };
    }

    public ResponseDto ToResponseDto(Models.Entities.Response r)
    {
        return new ResponseDto
        {
            Id = r.Id,
            RecruitmentId = r.RecruitmentId,
            ResponserId = r.ResponserId,
            ResponseStatus = r.ResponseStatus.ToDtoString(),
            CreatedAt = r.CreatedAt.ToString("o"),
            UpdatedAt = r.UpdatedAt.ToString("o"),
            Responser = ToUserBriefDto(r.Responser),
        };
    }

    public MessageDto ToMessageDto(Message m)
    {
        return new MessageDto
        {
            Id = m.Id,
            ChatId = m.ChatId,
            SenderId = m.SenderId,
            ReceiverId = m.ReceiverId,
            Content = m.Content,
            CreatedAt = m.CreatedAt.ToString("o"),
            Sender = ToUserBriefDto(m.Sender),
            Receiver = ToUserBriefDto(m.Receiver),
        };
    }

    public ChatBriefDto ToChatBriefDto(Chat c, long currentUserId)
    {
        var otherUser = c.RecruiterId == currentUserId ? c.Responser : c.Recruiter;
        var lastMsg = c.Messages.MaxBy(m => m.CreatedAt);
        return new ChatBriefDto
        {
            Id = c.Id,
            OtherUserAvatar = otherUser.Avatar,
            OtherUserName = otherUser.Nickname == "" ? otherUser.Username : otherUser.Nickname,
            LastMessageContent = lastMsg?.Content ?? "",
            LastMessageAt = lastMsg?.CreatedAt.ToString("o") ?? c.CreatedAt.ToString("o"),
            CreatedAt = c.CreatedAt.ToString("o"),
        };
    }

    public ChatDto ToChatDto(Chat c, long currentUserId)
    {
        var otherUser = c.RecruiterId == currentUserId ? c.Responser : c.Recruiter;
        var lastMsg = c.Messages?.MaxBy(m => m.CreatedAt);

        int unread = 0; // Unread tracking can be added later

        // Build users array with sent_message status
        var recruiterSent = c.Messages?.Any(m => m.SenderId == c.RecruiterId) ?? false;
        var responserSent = c.Messages?.Any(m => m.SenderId == c.ResponserId) ?? false;

        var users = new[]
        {
            new ChatUserDto { UserId = c.RecruiterId, SentMessage = recruiterSent },
            new ChatUserDto { UserId = c.ResponserId, SentMessage = responserSent },
        };

        return new ChatDto
        {
            Id = c.Id,
            RecruitmentId = c.RecruitmentId,
            RecruitmentTitle = c.Recruitment?.Title ?? "",
            OtherUser = otherUser != null ? ToUserBriefDto(otherUser) : new UserBriefDto { Id = otherUser?.Id ?? 0, Username = "", Nickname = "" },
            LastMessage = lastMsg != null ? ToMessageDto(lastMsg) : null,
            UnreadCount = unread,
            ChatStatus = c.ChatStatus.ToDtoString(),
            NewMessageAt = c.NewMessageAt?.ToString("o") ?? "",
            Users = users,
            Recruitment = c.Recruitment != null ? ToRecruitmentBriefDto(c.Recruitment) : new RecruitmentBriefDto { Id = c.RecruitmentId, Title = "" },
        };
    }
}