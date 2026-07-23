using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Dtos.Response;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResponseController : ControllerBase
{
    private readonly ResponseService _service;

    public ResponseController(ResponseService service)
    {
        _service = service;
    }

    // 回应招募  POST /api/response
    [HttpPost]
    public async Task<ActionResult<ResponseDto>> Create(
        [FromBody] CreateResponseDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _service.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // 单条回应详情  GET /api/response/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseDto>> GetById(long id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id, GetUserId());
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("不存在"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Forbid();
        }
    }

    // 收到的回应列表  GET /api/response/inbox?page=1&pageSize=20&recruitmentId=
    [HttpGet("inbox")]
    public async Task<ActionResult<PagedResult<ResponseDto>>> GetInbox(
        [FromQuery] long? recruitmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var validation = ValidatePaging(page, pageSize);
        if (validation != null) return validation;
        var userId = GetUserId();
        var result = await _service.GetInboxAsync(userId, recruitmentId, page, pageSize);
        return Ok(result);
    }

    // 发出的回应列表  GET /api/response/outbox?page=1&pageSize=20
    [HttpGet("outbox")]
    public async Task<ActionResult<PagedResult<ResponseDto>>> GetOutbox(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var validation = ValidatePaging(page, pageSize);
        if (validation != null) return validation;
        var userId = GetUserId();
        var result = await _service.GetOutboxAsync(userId, page, pageSize);
        return Ok(result);
    }

    // 撤回回应  DELETE /api/response/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> Cancel(long id)
    {
        try
        {
            var userId = GetUserId();
            await _service.CancelAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // 接受回应  PATCH /api/response/{id}/accept
    [HttpPatch("{id}/accept")]
    public async Task<ActionResult> Accept(long id)
    {
        try
        {
            var userId = GetUserId();
            await _service.AcceptAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // 拒绝回应  PATCH /api/response/{id}/reject
    [HttpPatch("{id}/reject")]
    public async Task<ActionResult> Reject(long id)
    {
        try
        {
            var userId = GetUserId();
            await _service.RejectAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // 分页参数校验
    private static BadRequestObjectResult? ValidatePaging(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return new BadRequestObjectResult(new { error = "page 必须 >= 1，pageSize 必须在 1-100 之间" });
        return null;
    }

    // 从 JWT Token 中提取当前用户 ID
    private long GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? throw new InvalidOperationException("Token 中未包含用户标识");
        return long.Parse(claim.Value);
    }
}
