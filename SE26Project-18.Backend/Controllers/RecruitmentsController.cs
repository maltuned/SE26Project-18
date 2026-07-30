using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class RecruitmentsController : ControllerBase
{
    private readonly IRecruitmentService _recruitmentService;

    public RecruitmentsController(IRecruitmentService recruitmentService)
    {
        _recruitmentService = recruitmentService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RecruitmentDetailDto>>>> GetRecruitments(
        [FromQuery] string gameName = "",
        [FromQuery] long[] gameTags = null!,
        [FromQuery] long[] recruitmentTags = null!)
    {
        var result = await _recruitmentService.GetRecruitmentsAsync(gameName, gameTags ?? [], recruitmentTags ?? []);
        return Ok(ApiResponse<List<RecruitmentDetailDto>>.Success(result));
    }

    [HttpGet("by-game")]
    public async Task<ActionResult<ApiResponse<List<RecruitmentDetailDto>>>> GetRecruitmentsByGame([FromQuery] long gameId)
    {
        var result = await _recruitmentService.GetRecruitmentsByGameAsync(gameId);
        return Ok(ApiResponse<List<RecruitmentDetailDto>>.Success(result));
    }

    [HttpGet("by-id")]
    public async Task<ActionResult<ApiResponse<RecruitmentDetailDto>>> GetRecruitmentById([FromQuery] long id)
    {
        var result = await _recruitmentService.GetRecruitmentByIdAsync(id);
        if (result == null)
            return Ok(ApiResponse<RecruitmentDetailDto>.Fail("招募不存在", 404));
        return Ok(ApiResponse<RecruitmentDetailDto>.Success(result));
    }

    [HttpGet("by-chat")]
    public async Task<ActionResult<ApiResponse<RecruitmentDetailDto>>> GetRecruitmentByChat([FromQuery] long chatId)
    {
        var result = await _recruitmentService.GetRecruitmentByChatIdAsync(chatId);
        if (result == null)
        {
            // 返回id为0的空dto表示无招募关联
            return Ok(ApiResponse<RecruitmentDetailDto>.Success(new RecruitmentDetailDto()));
        }
        return Ok(ApiResponse<RecruitmentDetailDto>.Success(result));
    }

    [HttpGet("by-publisher")]
    public async Task<ActionResult<ApiResponse<List<RecruitmentDetailDto>>>> GetRecruitmentsByPublisher([FromQuery] long publisherId)
    {
        var result = await _recruitmentService.GetRecruitmentsByPublisherIdAsync(publisherId);
        return Ok(ApiResponse<List<RecruitmentDetailDto>>.Success(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RecruitmentDetailDto>>> CreateRecruitment([FromBody] RecruitmentDto dto)
    {
        try
        {
            var result = await _recruitmentService.CreateRecruitmentAsync(dto);
            return Ok(ApiResponse<RecruitmentDetailDto>.Success(result, "创建成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return Ok(ApiResponse<RecruitmentDetailDto>.Fail(ex.Message, 404));
        }
    }

    [HttpPost("update")]
    public async Task<ActionResult<ApiResponse<RecruitmentDetailDto>>> UpdateRecruitment([FromBody] UpdateRecruitmentRequest request)
    {
        var result = await _recruitmentService.UpdateRecruitmentAsync(request.Id, request.Data);
        if (result == null)
            return Ok(ApiResponse<RecruitmentDetailDto>.Fail("招募不存在", 404));
        return Ok(ApiResponse<RecruitmentDetailDto>.Success(result, "更新成功"));
    }

    [HttpPost("delete")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRecruitment([FromBody] IdRequest request)
    {
        var result = await _recruitmentService.DeleteRecruitmentAsync(request.Id);
        return Ok(ApiResponse<bool>.Success(result, result ? "删除成功" : "招募不存在"));
    }

    [HttpPost("{id:long}/views")]
    public async Task<ActionResult<ApiResponse<bool>>> RecordView(long id, CancellationToken ct)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!long.TryParse(value, out var userId))
            return Ok(ApiResponse<bool>.Fail("未认证", 401));

        var result = await _recruitmentService.RecordViewAsync(userId, id, ct);
        return result
            ? Ok(ApiResponse<bool>.Success(true, "浏览记录成功"))
            : Ok(ApiResponse<bool>.Fail("招募不存在", 404));
    }
}

public class UpdateRecruitmentRequest
{
    public long Id { get; set; }
    public Dictionary<string, object> Data { get; set; } = [];
}

public class IdRequest
{
    public long Id { get; set; }
}
