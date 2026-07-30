using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/recruitments")]
public sealed class RecruitmentController : ControllerBase
{
    private readonly IRecruitmentService _recruitmentService;

    private readonly IResponseService _responseService;

    public RecruitmentController(
        IRecruitmentService recruitmentService,
        IResponseService responseService
    )
    {
        _recruitmentService = recruitmentService;
        _responseService = responseService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<RecruitmentResponse>>> Search(
        [FromQuery] RecruitmentQueryRequest request,
        CancellationToken ct
    )
    {
        return Ok(await _recruitmentService.SearchAsync(GetCurrentUserId(), request, ct));
    }

    [HttpGet("recruiters/{recruiterId:long}")]
    public async Task<ActionResult<PagedResponse<RecruitmentResponse>>> GetByRecruiter(
        long recruiterId,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] RecruitmentStatus? status = null,
        CancellationToken ct = default
    )
    {
        return Ok(
            await _recruitmentService.GetByRecruiterAsync(
                recruiterId,
                page,
                pageSize,
                status,
                ct
            )
        );
    }

    [HttpGet("{id:long}", Name = "GetRecruitmentById")]
    public async Task<ActionResult<RecruitmentResponse>> GetById(long id, CancellationToken ct)
    {
        return Ok(await _recruitmentService.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<RecruitmentResponse>> Create(
        CreateRecruitmentRequest request,
        CancellationToken ct
    )
    {
        var recruitment = await _recruitmentService.CreateAsync(GetCurrentUserId(), request, ct);
        return CreatedAtRoute("GetRecruitmentById", new { id = recruitment.Id }, recruitment);
    }

    [HttpPatch("{id:long}")]
    public async Task<ActionResult<RecruitmentResponse>> Update(
        long id,
        UpdateRecruitmentRequest request,
        CancellationToken ct
    )
    {
        return Ok(await _recruitmentService.UpdateAsync(GetCurrentUserId(), id, request, ct));
    }

    [HttpPost("{id:long}/close")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> ForceClose(long id, CancellationToken ct)
    {
        await _recruitmentService.ForceCloseAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/responses")]
    public async Task<ActionResult<ResponseResponse>> CreateResponse(long id, CancellationToken ct)
    {
        var response = await _responseService.CreateAsync(GetCurrentUserId(), id, ct);
        return CreatedAtRoute("GetResponseById", new { id = response.Id }, response);
    }

    [HttpPost("{id:long}/views")]
    public async Task<IActionResult> RecordView(long id, CancellationToken ct)
    {
        await _recruitmentService.RecordViewAsync(GetCurrentUserId(), id, ct);
        return NoContent();
    }

    private long GetCurrentUserId()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            throw new AuthenticationException("Token does not contain a valid user identifier.");
        }

        return userId;
    }
}
