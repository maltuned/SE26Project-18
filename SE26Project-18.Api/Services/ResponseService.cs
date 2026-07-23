using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public sealed class ResponseService : IResponseService
{
    private readonly AppDbContext _db;

    public ResponseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ResponseResponse> CreateAsync(long userId, CreateResponseRequest request)
    {
        var recruitment =
            await _db
                .Recruitments.Include(r => r.Recruiter)
                .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId)
            ?? throw new KeyNotFoundException("Recruitment not found.");

        EnsureRecruitmentCanProcess(recruitment);

        if (recruitment.Recruiter.Id == userId)
            throw new InvalidOperationException(
                "Recruiters cannot respond to their own recruitment."
            );

        var responder =
            await _db.Users.FindAsync(userId) ?? throw new KeyNotFoundException("User not found.");

        var exists = await _db.Responses.AnyAsync(r =>
            r.Recruitment.Id == request.RecruitmentId && r.Responder.Id == userId
        );
        if (exists)
            throw new InvalidOperationException("A response already exists for this recruitment.");

        var response = new Response(recruitment, responder);
        recruitment.Responses.Add(response);
        await _db.SaveChangesAsync();

        return response.ToResponse();
    }

    public async Task<ResponseResponse> GetByIdAsync(long responseId, long userId)
    {
        var response =
            await BaseQuery().FirstOrDefaultAsync(r => r.Id == responseId)
            ?? throw new KeyNotFoundException("Response not found.");

        if (response.Responder.Id != userId && response.Recruitment.Recruiter.Id != userId)
            throw new UnauthorizedAccessException();

        return response.ToResponse();
    }

    public async Task<ResponseResponse> AcceptAsync(long responseId, long recruiterId)
    {
        var response = await GetForDecisionAsync(responseId, recruiterId);
        EnsureRecruitmentCanProcess(response.Recruitment);

        response.Accept();
        response.Recruitment.AddParticipant();
        await _db.SaveChangesAsync();

        return response.ToResponse();
    }

    public async Task<ResponseResponse> RejectAsync(long responseId, long recruiterId)
    {
        var response = await GetForDecisionAsync(responseId, recruiterId);
        EnsureRecruitmentCanProcess(response.Recruitment);

        response.Reject();
        await _db.SaveChangesAsync();

        return response.ToResponse();
    }

    private async Task<Response> GetForDecisionAsync(long responseId, long recruiterId)
    {
        var response =
            await BaseQuery(tracking: true).FirstOrDefaultAsync(r => r.Id == responseId)
            ?? throw new KeyNotFoundException("Response not found.");

        if (response.Recruitment.Recruiter.Id != recruiterId)
            throw new UnauthorizedAccessException();

        return response;
    }

    private static void EnsureRecruitmentCanProcess(Recruitment recruitment)
    {
        if (recruitment.Status != RecruitmentStatus.Open)
            throw new InvalidOperationException("Recruitment is closed.");

        if (recruitment.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Recruitment has expired.");

        if (recruitment.CurrParticipants >= recruitment.MaxParticipants)
            throw new InvalidOperationException("Recruitment is full.");
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
