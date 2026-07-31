using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SE26Project_18.Backend.Controllers;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Controllers;

public class ControllerCoverageTests
{
    [Fact]
    public async Task Admin_GetAllReports_ConvertsStatusFilter()
    {
        var services = new AdminMocks();
        services.Report.Setup(x => x.GetAllAsync(ReportStatus.Resolved)).ReturnsAsync([]);

        var response = Response(await services.Create().GetAllReports("resolved"));

        Assert.Equal(200, response.Status);
        services.Report.Verify(x => x.GetAllAsync(ReportStatus.Resolved), Times.Once);
    }

    [Theory]
    [InlineData("recruitment-found", "Raid night")]
    [InlineData("recruitment-missing", "招募不存在")]
    [InlineData("user-found", "Target user")]
    [InlineData("user-missing", "用户不存在")]
    [InlineData("chat-found", "login-name")]
    [InlineData("chat-missing", "聊天不存在")]
    [InlineData("review-found", "review text")]
    [InlineData("review-missing", "评价不存在")]
    [InlineData("unknown", "99")]
    public async Task Admin_GetReportTarget_ResolvesEveryTargetBranch(string scenario, string expectedText)
    {
        var services = new AdminMocks();
        var targetType = scenario.Split('-')[0] switch
        {
            "recruitment" => ReportTargetType.Recruitment,
            "user" => ReportTargetType.User,
            "chat" => ReportTargetType.Chat,
            "review" => ReportTargetType.Review,
            _ => (ReportTargetType)99,
        };
        services.Report.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(new Report
        {
            ReporterId = 2,
            TargetId = 99,
            TargetType = targetType,
            ViolationType = ViolationType.Other,
        });

        if (scenario == "recruitment-found")
            services.Recruitment.Setup(x => x.GetRecruitmentByIdAsync(99))
                .ReturnsAsync(new RecruitmentDetailDto { Id = 99, Title = "Raid night" });
        if (scenario == "user-found")
            services.User.Setup(x => x.GetUserByIdAsync(99))
                .ReturnsAsync(new UserDto { Id = 99, Nickname = "Target user" });
        if (scenario == "chat-found")
        {
            services.Chat.Setup(x => x.GetChatByIdAsync(99, 0)).ReturnsAsync(new ChatDto
            {
                Id = 99,
                RecruitmentTitle = "Team chat",
                ChatStatus = "正常",
                OtherUser = new UserBriefDto { Nickname = "Participant" },
            });
            services.Message.Setup(x => x.GetMessagesByChatAsync(99)).ReturnsAsync(
            [
                new MessageDto { Id = 1, Content = "a", Sender = new UserBriefDto { Nickname = "Alias" } },
                new MessageDto { Id = 2, Content = "b", Sender = new UserBriefDto { Nickname = null!, Username = "login-name" } },
                new MessageDto { Id = 3, Content = "c", Sender = null! },
            ]);
        }
        if (scenario == "review-found")
            services.Review.Setup(x => x.GetReviewContentAsync(99)).ReturnsAsync("review text");

        var response = Response(await services.Create().GetReportTarget(4));
        var json = JsonSerializer.Serialize(response.Data, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });

        Assert.Equal(200, response.Status);
        Assert.Contains(expectedText, json);
    }

    [Fact]
    public async Task Admin_HandleReport_SucceedsWithoutNotification_WhenReportDisappears()
    {
        var services = new AdminMocks();
        services.Report.Setup(x => x.UpdateStatusAsync(8, ReportStatus.Resolved, 7)).ReturnsAsync(true);
        services.Report.Setup(x => x.GetByIdAsync(8)).ReturnsAsync((Report?)null);

        var response = Response(await services.Create().HandleReport(8, new HandleReportRequest { Status = "已处理" }));

        Assert.Equal(200, response.Status);
        Assert.True(response.Data);
        services.Notification.Verify(x => x.CreateAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Admin_HandleReport_TruncatesReviewName_AndDescribesRejection()
    {
        var services = new AdminMocks();
        const string content = "1234567890123456789012345";
        services.Report.Setup(x => x.UpdateStatusAsync(8, ReportStatus.Rejected, 7)).ReturnsAsync(true);
        services.Report.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(new Report
        {
            ReporterId = 12,
            TargetId = 3,
            TargetType = ReportTargetType.Review,
            ViolationType = ViolationType.Other,
        });
        services.Review.Setup(x => x.GetReviewContentAsync(3)).ReturnsAsync(content);

        var response = Response(await services.Create().HandleReport(8, new HandleReportRequest { Status = "驳回" }));

        Assert.Equal(200, response.Status);
        services.Notification.Verify(x => x.CreateAsync(12, "举报处理结果",
            It.Is<string>(body => body.Contains("12345678901234567890...") && body.Contains("驳回"))), Times.Once);
    }

    [Fact]
    public async Task Admin_HandleFeedback_TruncatesLongContentInNotification()
    {
        var services = new AdminMocks();
        services.Feedback.Setup(x => x.UpdateStatusAsync(5, FeedbackStatus.Resolved, 7)).ReturnsAsync(true);
        services.Feedback.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(new Feedback
        {
            UserId = 14,
            Type = FeedbackType.ExperienceFeedback,
            Content = "1234567890123456789012345",
        });

        var response = Response(await services.Create().HandleFeedback(5, new HandleFeedbackRequest { Status = "已处理" }));

        Assert.Equal(200, response.Status);
        services.Notification.Verify(x => x.CreateAsync(14, "反馈处理结果",
            It.Is<string>(body => body.Contains("12345678901234567890…"))), Times.Once);
    }

    [Fact]
    public async Task Admin_SendNotification_SendsOnlyToRequestedUser()
    {
        var services = new AdminMocks();

        var response = Response(await services.Create().SendNotification(new SendNotificationRequest
        {
            UserId = 33,
            Title = "Maintenance",
            Body = "At midnight",
        }));

        Assert.Equal(200, response.Status);
        services.Notification.Verify(x => x.CreateAsync(33, "Maintenance", "At midnight"), Times.Once);
        services.User.Verify(x => x.GetUsersAsync(), Times.Never);
    }

    [Fact]
    public async Task Admin_GameExceptionAndImageSuccessBranches_ReturnExpectedResponses()
    {
        var services = new AdminMocks();
        services.Game.Setup(x => x.CreateGameAsync(It.IsAny<GameRequestDto>()))
            .ThrowsAsync(new KeyNotFoundException("tag missing"));
        services.Game.Setup(x => x.UpdateGameImageAsync(2, "cover.jpg", "icon.png"))
            .ReturnsAsync(new GameDto { Id = 2, Name = "Game" });
        var controller = services.Create();

        var create = Response(await controller.CreateGame(new GameRequestDto { Name = "Game" }));
        var image = Response(await controller.UpdateGameImage(2,
            new UpdateGameImageRequest { Cover = "cover.jpg", Icon = "icon.png" }));

        Assert.Equal(404, create.Status);
        Assert.Equal(200, image.Status);
        Assert.Equal(2, image.Data!.Id);
    }

    [Fact]
    public async Task Admin_UpdateReviewStatus_AcceptsHiddenStatus()
    {
        var services = new AdminMocks();
        services.Review.Setup(x => x.UpdateStatusAsync(6, ReviewStatus.Hidden)).ReturnsAsync(true);

        var response = Response(await services.Create().UpdateReviewStatus(6,
            new UpdateReviewStatusRequest { Status = "隐藏" }));

        Assert.Equal(200, response.Status);
        Assert.True(response.Data);
    }

    [Theory]
    [InlineData(true, false, 403)]
    [InlineData(false, true, 404)]
    [InlineData(false, false, 200)]
    public async Task Users_GetUserProfile_HandlesPrivacyMissingAndSuccess(bool isPrivate, bool missing, int expectedStatus)
    {
        var users = new Mock<IUserService>();
        UserDto? user = missing ? null : new UserDto { Id = 22, Nickname = "Visible user" };
        users.Setup(x => x.GetUserProfileAsync(12, 22)).ReturnsAsync((user, isPrivate));
        var controller = WithClaims(new UsersController(users.Object), new Claim(JwtRegisteredClaimNames.Sub, "12"));

        var response = Response(await controller.GetUserProfile(22));

        Assert.Equal(expectedStatus, response.Status);
        users.Verify(x => x.GetUserProfileAsync(12, 22), Times.Once);
    }

    [Fact]
    public async Task Users_GetSettings_RejectsInvalidClaimWithoutCallingService()
    {
        var users = new Mock<IUserService>();
        var controller = WithClaims(new UsersController(users.Object), new Claim(ClaimTypes.NameIdentifier, "not-a-number"));

        var response = Response(await controller.GetSettings());

        Assert.Equal(401, response.Status);
        users.Verify(x => x.GetUserSettingsAsync(It.IsAny<long>()), Times.Never);
    }

    [Theory]
    [InlineData(true, 200)]
    [InlineData(false, 404)]
    public async Task Users_GetSettings_HandlesExistingAndMissingSettings(bool exists, int expectedStatus)
    {
        var users = new Mock<IUserService>();
        users.Setup(x => x.GetUserSettingsAsync(9)).ReturnsAsync(exists ? new UserSettingsDto { PushEnabled = true } : null);
        var controller = WithClaims(new UsersController(users.Object), new Claim(ClaimTypes.NameIdentifier, "9"));

        var response = Response(await controller.GetSettings());

        Assert.Equal(expectedStatus, response.Status);
    }

    [Theory]
    [InlineData(true, 200)]
    [InlineData(false, 404)]
    public async Task Users_UpdateSettings_HandlesExistingAndMissingSettings(bool exists, int expectedStatus)
    {
        var users = new Mock<IUserService>();
        var requested = new UserSettingsDto { DarkMode = true, ProfileVisible = true };
        users.Setup(x => x.UpdateUserSettingsAsync(9, requested)).ReturnsAsync(exists ? requested : null);
        var controller = WithClaims(new UsersController(users.Object), new Claim(JwtRegisteredClaimNames.Sub, "9"));

        var response = Response(await controller.UpdateSettings(requested));

        Assert.Equal(expectedStatus, response.Status);
    }

    [Fact]
    public async Task Auth_Me_UsesSubjectClaim_AndHandlesMissingUser()
    {
        var auth = new Mock<IAuthService>();
        var users = new Mock<IUserService>();
        users.Setup(x => x.GetUserByIdAsync(17)).ReturnsAsync((UserDto?)null);
        var controller = WithClaims(new AuthController(auth.Object, users.Object), new Claim(JwtRegisteredClaimNames.Sub, "17"));

        var response = Response(await controller.Me());

        Assert.Equal(404, response.Status);
        users.Verify(x => x.GetUserByIdAsync(17), Times.Once);
    }

    [Fact]
    public async Task Auth_Refresh_MapsServiceFailureToUnauthorizedResponse()
    {
        var auth = new Mock<IAuthService>();
        auth.Setup(x => x.RefreshAsync("expired")).ThrowsAsync(new InvalidOperationException("refresh expired"));
        var controller = new AuthController(auth.Object, Mock.Of<IUserService>());

        var response = Response(await controller.Refresh(new RefreshRequest { RefreshToken = "expired" }));

        Assert.Equal(401, response.Status);
        Assert.Equal("refresh expired", response.Message);
    }

    [Fact]
    public async Task Auth_Logout_RejectsInvalidClaim()
    {
        var auth = new Mock<IAuthService>();
        var controller = WithClaims(new AuthController(auth.Object, Mock.Of<IUserService>()),
            new Claim(ClaimTypes.NameIdentifier, "invalid"));

        var response = Response(await controller.Logout(new RefreshRequest { RefreshToken = "token" }));

        Assert.Equal(401, response.Status);
        auth.Verify(x => x.LogoutAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Auth_ChangePassword_UsesSubjectClaim_OnSuccess()
    {
        var auth = new Mock<IAuthService>();
        var controller = WithClaims(new AuthController(auth.Object, Mock.Of<IUserService>()),
            new Claim(JwtRegisteredClaimNames.Sub, "23"));

        var response = Response(await controller.ChangePassword(new ChangePasswordRequest
        {
            OldPassword = "old",
            NewPassword = "new",
        }));

        Assert.Equal(200, response.Status);
        Assert.True(response.Data);
        auth.Verify(x => x.ChangePasswordAsync(23, "old", "new"), Times.Once);
    }

    [Fact]
    public async Task Auth_ChangePassword_MapsValidationFailureToBadRequestResponse()
    {
        var auth = new Mock<IAuthService>();
        auth.Setup(x => x.ChangePasswordAsync(23, "wrong", "new"))
            .ThrowsAsync(new InvalidOperationException("old password invalid"));
        var controller = WithClaims(new AuthController(auth.Object, Mock.Of<IUserService>()),
            new Claim(ClaimTypes.NameIdentifier, "23"));

        var response = Response(await controller.ChangePassword(new ChangePasswordRequest
        {
            OldPassword = "wrong",
            NewPassword = "new",
        }));

        Assert.Equal(400, response.Status);
        Assert.Equal("old password invalid", response.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("bad")]
    public async Task Report_SubmitReport_RejectsMissingOrInvalidIdentity(string? claimValue)
    {
        var report = new Mock<IReportService>();
        var controller = CreateReportController(report: report);
        if (claimValue != null)
            WithClaims(controller, new Claim(ClaimTypes.NameIdentifier, claimValue));
        else
            WithClaims(controller);

        var response = Response(await controller.SubmitReport(new ReportDto
        {
            TargetType = "用户",
            TargetId = 2,
            ViolationType = "其他",
            Content = "details",
        }));

        Assert.Equal(401, response.Status);
        report.Verify(x => x.SubmitReportAsync(It.IsAny<long>(), It.IsAny<ReportTargetType>(), It.IsAny<long>(),
            It.IsAny<ViolationType>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Report_SubmitReport_RejectsInvalidViolationType()
    {
        var controller = WithClaims(CreateReportController(), new Claim(ClaimTypes.NameIdentifier, "1"));

        var response = Response(await controller.SubmitReport(new ReportDto
        {
            TargetType = "聊天",
            TargetId = 2,
            ViolationType = "unknown",
            Content = "details",
        }));

        Assert.Equal(400, response.Status);
        Assert.Contains("unknown", response.Message);
    }

    [Theory]
    [InlineData("招募", "Team up")]
    [InlineData("用户", "Reported user")]
    [InlineData("聊天", "Chat partner")]
    [InlineData("评价", "12345678901234567890...")]
    public async Task Report_SubmitReport_UsesResolvedTargetNameInNotification(string targetType, string expectedName)
    {
        var notifications = new Mock<INotificationService>();
        var recruitments = new Mock<IRecruitmentService>();
        var users = new Mock<IUserService>();
        var chats = new Mock<IChatService>();
        var reviews = new Mock<IReviewService>();
        recruitments.Setup(x => x.GetRecruitmentByIdAsync(5)).ReturnsAsync(new RecruitmentDetailDto { Title = "Team up" });
        users.Setup(x => x.GetUserByIdAsync(5)).ReturnsAsync(new UserDto { Nickname = "Reported user" });
        chats.Setup(x => x.GetChatByIdAsync(5, 0)).ReturnsAsync(new ChatDto
        {
            OtherUser = new UserBriefDto { Nickname = "Chat partner" },
        });
        reviews.Setup(x => x.GetReviewContentAsync(5)).ReturnsAsync("1234567890123456789012345");
        var controller = WithClaims(new ReportController(Mock.Of<IReportService>(), notifications.Object,
            recruitments.Object, users.Object, chats.Object, reviews.Object),
            new Claim(JwtRegisteredClaimNames.Sub, "11"));

        var response = Response(await controller.SubmitReport(new ReportDto
        {
            TargetType = targetType,
            TargetId = 5,
            ViolationType = "其他",
            Content = "details",
        }));

        Assert.Equal(200, response.Status);
        notifications.Verify(x => x.CreateAsync(11, "举报已提交",
            It.Is<string>(body => body.Contains(expectedName))), Times.Once);
    }

    [Fact]
    public async Task Report_SubmitReport_MapsServiceArgumentException()
    {
        var report = new Mock<IReportService>();
        report.Setup(x => x.SubmitReportAsync(3, ReportTargetType.User, 4, ViolationType.Fraud, "details"))
            .ThrowsAsync(new ArgumentException("already reported"));
        var controller = WithClaims(CreateReportController(report: report), new Claim(ClaimTypes.NameIdentifier, "3"));

        var response = Response(await controller.SubmitReport(new ReportDto
        {
            TargetType = "用户",
            TargetId = 4,
            ViolationType = "欺诈",
            Content = "details",
        }));

        Assert.Equal(400, response.Status);
        Assert.Equal("already reported", response.Message);
    }

    [Theory]
    [InlineData("covers", false, 403)]
    [InlineData("not-allowed", false, 200)]
    [InlineData("icons", true, 200)]
    public async Task Image_Upload_EnforcesAdminFoldersAndNormalizesUnknownFolder(string folder, bool isAdmin, int expectedStatus)
    {
        var images = new Mock<IImageService>();
        images.Setup(x => x.UploadWithNameAsync(It.IsAny<Stream>(), "icons/logo.png", "image/png"))
            .ReturnsAsync("icons/logo.png");
        images.Setup(x => x.UploadAsync(It.IsAny<Stream>(), "logo.png", "image/png", "general"))
            .ReturnsAsync("general/generated.png");
        var claims = isAdmin ? new[] { new Claim(ClaimTypes.Role, "Admin") } : [];
        var controller = WithClaims(new ImageController(images.Object), claims);

        var result = await controller.Upload(CreateFile("logo.png", contentType: "image/png"), folder,
            folder == "icons" ? "logo" : null);

        var response = expectedStatus == 403
            ? Assert.IsType<ApiResponse<string>>(Assert.IsType<UnauthorizedObjectResult>(result.Result).Value)
            : Assert.IsType<ApiResponse<string>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(expectedStatus, response.Status);
        if (folder == "not-allowed")
            images.Verify(x => x.UploadAsync(It.IsAny<Stream>(), "logo.png", "image/png", "general"), Times.Once);
    }

    [Theory]
    [InlineData("empty", 0L)]
    [InlineData("bad.exe", 100L)]
    [InlineData("large.jpg", 5242881L)]
    public async Task Image_UploadAvatar_RejectsInvalidFiles(string fileName, long length)
    {
        var controller = new ImageController(Mock.Of<IImageService>());

        var result = await controller.UploadAvatar(CreateFile(fileName, length), 4);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Image_UploadAvatar_ReplacesExistingAvatarWithStableName()
    {
        var images = new Mock<IImageService>();
        images.Setup(x => x.UploadWithNameAsync(It.IsAny<Stream>(), "avatars/4.webp", "image/webp"))
            .ReturnsAsync("avatars/4.webp");
        var controller = new ImageController(images.Object);

        var response = Response(await controller.UploadAvatar(CreateFile("avatar.webp", contentType: "image/webp"), 4));

        Assert.Equal("/Image/file/avatars/4.webp", response.Data);
        images.Verify(x => x.DeleteByPrefixAsync("avatars/4."), Times.Once);
        images.Verify(x => x.UploadWithNameAsync(It.IsAny<Stream>(), "avatars/4.webp", "image/webp"), Times.Once);
    }

    [Theory]
    [InlineData("picture.png", "image/png")]
    [InlineData("picture.gif", "image/gif")]
    [InlineData("picture.webp", "image/webp")]
    [InlineData("picture.JPG", "image/jpeg")]
    public async Task Image_GetFile_SelectsContentTypeFromExtension(string objectName, string expectedContentType)
    {
        var images = new Mock<IImageService>();
        images.Setup(x => x.GetStreamAsync(objectName)).ReturnsAsync(new MemoryStream([1, 2]));
        var controller = new ImageController(images.Object);

        var result = Assert.IsType<FileStreamResult>(await controller.GetFile(objectName));

        Assert.Equal(expectedContentType, result.ContentType);
    }

    [Theory]
    [InlineData(null, true, 401)]
    [InlineData("5", true, 200)]
    [InlineData("5", false, 404)]
    public async Task Recruitments_RecordView_HandlesAuthenticationAndServiceResult(
        string? userId, bool recorded, int expectedStatus)
    {
        var recruitments = new Mock<IRecruitmentService>();
        recruitments.Setup(x => x.RecordViewAsync(5, 20, It.IsAny<CancellationToken>())).ReturnsAsync(recorded);
        var controller = userId == null
            ? WithClaims(new RecruitmentsController(recruitments.Object))
            : WithClaims(new RecruitmentsController(recruitments.Object), new Claim(JwtRegisteredClaimNames.Sub, userId));

        var response = Response(await controller.RecordView(20, CancellationToken.None));

        Assert.Equal(expectedStatus, response.Status);
        if (userId == null)
            recruitments.Verify(x => x.RecordViewAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Messages_SendMessage_ReturnsCreatedMessage()
    {
        var messages = new Mock<IMessageService>();
        messages.Setup(x => x.SendMessageAsync(2, 3, 4, "hello"))
            .ReturnsAsync(new MessageDto { Id = 9, ChatId = 2, Content = "hello" });
        var controller = new MessagesController(messages.Object);

        var response = Response(await controller.SendMessage(new SendMessageRequest
        {
            ChatId = 2,
            SenderId = 3,
            ReceiverId = 4,
            Content = "hello",
        }));

        Assert.Equal(9, response.Data!.Id);
        Assert.Equal("发送成功", response.Message);
    }

    [Fact]
    public async Task Messages_MarkAsRead_ForwardsBothIdentifiers()
    {
        var messages = new Mock<IMessageService>();
        var controller = new MessagesController(messages.Object);

        var response = Response(await controller.MarkAsRead(new MarkReadRequest { ChatId = 6, UserId = 7 }));

        Assert.True(response.Data);
        messages.Verify(x => x.MarkAsReadAsync(6, 7), Times.Once);
    }

    [Fact]
    public async Task Feedback_SubmitFeedback_RejectsMissingIdentity()
    {
        var feedback = new Mock<IFeedbackService>();
        var controller = WithClaims(new FeedbackController(feedback.Object, Mock.Of<INotificationService>()));

        var response = Response(await controller.SubmitFeedback(new FeedbackDto
        {
            Type = "体验反馈",
            Content = "details",
        }));

        Assert.Equal(401, response.Status);
        feedback.Verify(x => x.SubmitFeedbackAsync(It.IsAny<long>(), It.IsAny<FeedbackType>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Feedback_SubmitFeedback_MapsArgumentException()
    {
        var feedback = new Mock<IFeedbackService>();
        feedback.Setup(x => x.SubmitFeedbackAsync(8, FeedbackType.ExperienceFeedback, "duplicate"))
            .ThrowsAsync(new ArgumentException("duplicate feedback"));
        var controller = WithClaims(new FeedbackController(feedback.Object, Mock.Of<INotificationService>()),
            new Claim(JwtRegisteredClaimNames.Sub, "8"));

        var response = Response(await controller.SubmitFeedback(new FeedbackDto
        {
            Type = "体验反馈",
            Content = "duplicate",
        }));

        Assert.Equal(400, response.Status);
        Assert.Equal("duplicate feedback", response.Message);
    }

    [Fact]
    public async Task Feedback_SubmitFeedback_TruncatesLongNotificationPreview()
    {
        var notifications = new Mock<INotificationService>();
        var controller = WithClaims(new FeedbackController(Mock.Of<IFeedbackService>(), notifications.Object),
            new Claim(ClaimTypes.NameIdentifier, "8"));

        var response = Response(await controller.SubmitFeedback(new FeedbackDto
        {
            Type = "体验反馈",
            Content = "1234567890123456789012345",
        }));

        Assert.Equal(200, response.Status);
        notifications.Verify(x => x.CreateAsync(8, "反馈已提交",
            It.Is<string>(body => body.Contains("12345678901234567890…"))), Times.Once);
    }

    [Fact]
    public async Task Review_HasReviewed_RejectsMissingIdentity()
    {
        var reviews = new Mock<IReviewService>();
        var controller = WithClaims(CreateReviewController(reviews: reviews));

        var response = Response(await controller.HasReviewed(4));

        Assert.Equal(401, response.Status);
        reviews.Verify(x => x.HasReviewedAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Review_CreateReview_AllowsOpenChat_AndFallsBackToUsername()
    {
        var reviews = new Mock<IReviewService>();
        var notifications = new Mock<INotificationService>();
        var users = new Mock<IUserService>();
        var chats = new Mock<IChatService>();
        chats.Setup(x => x.GetChatByUsersAsync(It.Is<long[]>(ids => ids.SequenceEqual(new long[] { 3, 4 }))))
            .ReturnsAsync(new ChatDto { ChatStatus = "正常" });
        users.Setup(x => x.GetUserByIdAsync(3)).ReturnsAsync(new UserDto { Nickname = null!, Username = "reviewer-login" });
        var controller = WithClaims(new ReviewController(reviews.Object, notifications.Object, users.Object, chats.Object),
            new Claim(JwtRegisteredClaimNames.Sub, "3"));

        var response = Response(await controller.CreateReview(new CreateReviewDto { RevieweeId = 4, Content = "helpful" }));

        Assert.Equal(200, response.Status);
        reviews.Verify(x => x.CreateAsync(3, 4, "helpful"), Times.Once);
        notifications.Verify(x => x.CreateAsync(4, "收到新评价", "收到reviewer-login的评价"), Times.Once);
    }

    [Theory]
    [InlineData("隐藏", true, 200)]
    [InlineData("显示", false, 404)]
    public async Task Review_UpdateStatus_HandlesBothStatusesAndServiceOutcomes(string status, bool updated, int expectedStatus)
    {
        var reviews = new Mock<IReviewService>();
        reviews.Setup(x => x.UpdateStatusAsync(10, status == "隐藏" ? ReviewStatus.Hidden : ReviewStatus.Visible))
            .ReturnsAsync(updated);
        var controller = CreateReviewController(reviews: reviews);

        var response = Response(await controller.UpdateStatus(10, new UpdateReviewStatusDto { Status = status }));

        Assert.Equal(expectedStatus, response.Status);
    }

    private static ReportController CreateReportController(
        Mock<IReportService>? report = null,
        Mock<INotificationService>? notifications = null,
        Mock<IRecruitmentService>? recruitments = null,
        Mock<IUserService>? users = null,
        Mock<IChatService>? chats = null,
        Mock<IReviewService>? reviews = null) =>
        new((report ?? new()).Object, (notifications ?? new()).Object, (recruitments ?? new()).Object,
            (users ?? new()).Object, (chats ?? new()).Object, (reviews ?? new()).Object);

    private static ReviewController CreateReviewController(
        Mock<IReviewService>? reviews = null,
        Mock<INotificationService>? notifications = null,
        Mock<IUserService>? users = null,
        Mock<IChatService>? chats = null) =>
        new((reviews ?? new()).Object, (notifications ?? new()).Object, (users ?? new()).Object, (chats ?? new()).Object);

    private static TController WithClaims<TController>(TController controller, params Claim[] claims)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")),
            },
        };
        return controller;
    }

    private static ApiResponse<T> Response<T>(ActionResult<ApiResponse<T>> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<ApiResponse<T>>(ok.Value);
    }

    private static IFormFile CreateFile(string fileName, long length = 100, string contentType = "image/jpeg")
    {
        var file = new Mock<IFormFile>();
        file.SetupGet(x => x.FileName).Returns(fileName);
        file.SetupGet(x => x.Length).Returns(length);
        file.SetupGet(x => x.ContentType).Returns(contentType);
        file.Setup(x => x.OpenReadStream()).Returns(() => new MemoryStream(new byte[Math.Min(length, 100)]));
        return file.Object;
    }

    private sealed class AdminMocks
    {
        public Mock<IAdminService> Admin { get; } = new();
        public Mock<IReportService> Report { get; } = new();
        public Mock<IFeedbackService> Feedback { get; } = new();
        public Mock<IUserService> User { get; } = new();
        public Mock<IRecruitmentService> Recruitment { get; } = new();
        public Mock<IGameService> Game { get; } = new();
        public Mock<IChatService> Chat { get; } = new();
        public Mock<IMessageService> Message { get; } = new();
        public Mock<INotificationService> Notification { get; } = new();
        public Mock<IReviewService> Review { get; } = new();
        public Mock<ITagService> Tag { get; } = new();

        public AdminController Create() => WithClaims(new AdminController(Admin.Object, Report.Object, Feedback.Object,
            User.Object, Recruitment.Object, Game.Object, Chat.Object, Message.Object, Notification.Object,
            Review.Object, Tag.Object), new Claim(ClaimTypes.NameIdentifier, "7"));
    }
}
