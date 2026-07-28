using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Dtos.Recruitment;
using SE26Project_18.Api.Exceptions;
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

    public RecruitmentController(IRecruitmentService recruitmentService, IResponseService responseService)
    {
        _recruitmentService = recruitmentService;
        _responseService = responseService;
    }

    // 列表/搜索
    [HttpGet]
    public async Task<ActionResult<List<RecruitmentListResponse>>> GetList(
        [FromQuery] string? gameName,
        [FromQuery] string? gameTagIds,
        [FromQuery] string? recruitmentTagIds,
        CancellationToken ct)
    {
        var gameTags = ParseIds(gameTagIds);
        var recTags = ParseIds(recruitmentTagIds);
        return Ok(await _recruitmentService.GetListAsync(gameName, gameTags, recTags, ct));
    }

    // 按发布者
    [HttpGet("by-publisher/{publisherId:long}")]
    public async Task<ActionResult<List<RecruitmentListResponse>>> GetByPublisher(long publisherId, CancellationToken ct)
    {
        return Ok(await _recruitmentService.GetByPublisherIdAsync(publisherId, ct));
    }

    // 按游戏
    [HttpGet("by-game/{gameId:long}")]
    public async Task<ActionResult<List<RecruitmentListResponse>>> GetByGame(long gameId, CancellationToken ct)
    {
        return Ok(await _recruitmentService.GetByGameIdAsync(gameId, ct));
    }

    // 按聊天
    [HttpGet("by-chat/{chatId:long}")]
    public async Task<ActionResult<RecruitmentListResponse?>> GetByChat(long chatId, CancellationToken ct)
    {
        var result = await _recruitmentService.GetByChatIdAsync(chatId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // 详情
    [HttpGet("{id:long}")]
    public async Task<ActionResult<RecruitmentDetailResponse>> GetById(long id, CancellationToken ct)
    {
        return Ok(await _recruitmentService.GetByIdAsync(id, ct));
    }

    // 创建
    [HttpPost]
    public async Task<ActionResult<RecruitmentDetailResponse>> Create(
        [FromBody] CreateRecruitmentRequest req, CancellationToken ct)
    {
        var result = await _recruitmentService.CreateAsync(GetCurrentUserId(), req, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // 修改
    [HttpPut("{id:long}")]
    public async Task<ActionResult<RecruitmentDetailResponse>> Update(
        long id, [FromBody] UpdateRecruitmentRequest req, CancellationToken ct)
    {
        return Ok(await _recruitmentService.UpdateAsync(id, GetCurrentUserId(), req, ct));
    }

    // 删除（软删除）
    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        await _recruitmentService.DeleteAsync(id, GetCurrentUserId(), ct);
        return NoContent();
    }

    // 回应招募
    [HttpPost("{recruitmentId:long}/responses")]
    public async Task<ActionResult<ResponseResponse>> CreateResponse(
        long recruitmentId, CancellationToken ct)
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

    private static List<long>? ParseIds(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return null;
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(long.Parse).ToList();
    }
}
