using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public sealed class ResponseController : ControllerBase
{
    private readonly IResponseService _service;

    public ResponseController(IResponseService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ResponseResponse>> Create(
        [FromBody] CreateResponseRequest request
    )
    {
        try
        {
            var response = await _service.CreateAsync(GetUserId(), request);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ResponseResponse>> GetById(long id)
    {
        try
        {
            return Ok(await _service.GetByIdAsync(id, GetUserId()));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPatch("{id:long}/accept")]
    public async Task<ActionResult<ResponseResponse>> Accept(long id)
    {
        try
        {
            return Ok(await _service.AcceptAsync(id, GetUserId()));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPatch("{id:long}/reject")]
    public async Task<ActionResult<ResponseResponse>> Reject(long id)
    {
        try
        {
            return Ok(await _service.RejectAsync(id, GetUserId()));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    private long GetUserId()
    {
        var claim =
            User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Token does not contain a user identifier.");
        return long.Parse(claim.Value);
    }
}
