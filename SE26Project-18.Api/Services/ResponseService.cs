using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

internal sealed class ResponseService : IResponseService
{
    private readonly AppDbContext _db;

    public ResponseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ResponseResponse> CreateAsync(
        long userId,
        long recruitmentId,
        CancellationToken ct
    )
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var recruitment =
            await _db
                .Recruitments.Include(r => r.Recruiter)
                .FirstOrDefaultAsync(r => r.Id == recruitmentId, ct)
            ?? throw new NotFoundException("Recruitment not found.");

        EnsureRecruitmentCanProcess(recruitment);

        if (recruitment.Recruiter.Id == userId)
            throw new ConflictException("Recruiters cannot respond to their own recruitment.");

        var responder =
            await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User not found.");

        var exists = await _db.Responses.AnyAsync(
            r => r.Recruitment.Id == recruitmentId && r.Responder.Id == userId,
            ct
        );
        if (exists)
            throw new ConflictException("A response already exists for this recruitment.");

        var response = new Response(recruitment, responder);
        recruitment.Responses.Add(response);

        var (user1, user2) =
            recruitment.Recruiter.Id < responder.Id
                ? (recruitment.Recruiter, responder)
                : (responder, recruitment.Recruiter);
        var chat = await _db.Chats.FirstOrDefaultAsync(
            c => c.User1.Id == user1.Id && c.User2.Id == user2.Id,
            ct
        );

        if (chat is null)
            _db.Chats.Add(new Chat(recruitment, user1, user2));
        else
            chat.Recruitment = recruitment;

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return response.ToResponse();
    }

    public async Task<ResponseResponse> GetByIdAsync(
        long responseId,
        long userId,
        CancellationToken ct
    )
    {
        var response =
            await BaseQuery().FirstOrDefaultAsync(r => r.Id == responseId, ct)
            ?? throw new NotFoundException("Response not found.");

        if (response.Responder.Id != userId && response.Recruitment.Recruiter.Id != userId)
            throw new ForbiddenException("You are not a participant in this response.");

        return response.ToResponse();
    }

    public async Task<ResponseResponse> AcceptAsync(
        long responseId,
        long recruiterId,
        CancellationToken ct
    )
    {
        var response = await GetForDecisionAsync(responseId, recruiterId, ct);
        EnsureRecruitmentCanProcess(response.Recruitment);

        response.Accept();
        response.Recruitment.AddParticipant();

        await _db.SaveChangesAsync(ct);

        return response.ToResponse();
    }

    public async Task<ResponseResponse> RejectAsync(
        long responseId,
        long recruiterId,
        CancellationToken ct
    )
    {
        var response = await GetForDecisionAsync(responseId, recruiterId, ct);
        EnsureRecruitmentCanProcess(response.Recruitment);

        response.Reject();
        await _db.SaveChangesAsync(ct);

        return response.ToResponse();
    }

    private async Task<Response> GetForDecisionAsync(
        long responseId,
        long recruiterId,
        CancellationToken ct
    )
    {
        var response =
            await BaseQuery(tracking: true).FirstOrDefaultAsync(r => r.Id == responseId, ct)
            ?? throw new NotFoundException("Response not found.");

        if (response.Recruitment.Recruiter.Id != recruiterId)
            throw new ForbiddenException("Only the recruitment recruiter can process responses.");

        return response;
    }

    private static void EnsureRecruitmentCanProcess(Recruitment recruitment)
    {
        if (recruitment.Status != RecruitmentStatus.Open)
            throw new ConflictException("Recruitment is closed.");

        if (recruitment.ExpiresAt <= DateTime.UtcNow)
            throw new ConflictException("Recruitment has expired.");

        if (recruitment.CurrParticipants >= recruitment.MaxParticipants)
            throw new ConflictException("Recruitment is full.");
    }

    private IQueryable<Response> BaseQuery(bool tracking = false)
    {
        var query = _db
            .Responses.Include(r => r.Recruitment)
                .ThenInclude(r => r.Recruiter)
            .Include(r => r.Responder);
        return tracking ? query : query.AsNoTracking();
    }
}
