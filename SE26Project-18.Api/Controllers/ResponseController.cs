using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/responses")]
public sealed class ResponseController : ControllerBase
{
    private readonly IResponseService _service;

    public ResponseController(IResponseService service)
    {
        _service = service;
    }

    [HttpGet("{id:long}", Name = "GetResponseById")]
    public async Task<ActionResult<ResponseResponse>> GetById(long id, CancellationToken ct)
    {
        return Ok(await _service.GetByIdAsync(id, GetCurrentUserId(), ct));
    }

    [HttpPost("{id:long}/accept")]
    public async Task<ActionResult<ResponseResponse>> Accept(long id, CancellationToken ct)
    {
        return Ok(await _service.AcceptAsync(id, GetCurrentUserId(), ct));
    }

    [HttpPost("{id:long}/reject")]
    public async Task<ActionResult<ResponseResponse>> Reject(long id, CancellationToken ct)
    {
        return Ok(await _service.RejectAsync(id, GetCurrentUserId(), ct));
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
