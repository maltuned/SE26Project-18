using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/recruitments")]
public sealed class RecruitmentController : ControllerBase
{
    private readonly IResponseService _responseService;

    public RecruitmentController(IResponseService responseService)
    {
        _responseService = responseService;
    }

    [HttpPost("{recruitmentId:long}/responses")]
    public async Task<ActionResult<ResponseResponse>> CreateResponse(
        long recruitmentId,
        CancellationToken ct
    )
    {
        var response = await _responseService.CreateAsync(GetCurrentUserId(), recruitmentId, ct);
        return CreatedAtRoute("GetResponseById", new { id = response.Id }, response);
    }

    private long GetCurrentUserId()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new AuthenticationException("Token does not contain a valid user identifier.");

        return userId;
    }
}
