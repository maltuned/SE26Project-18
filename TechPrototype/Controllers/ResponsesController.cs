using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class ResponsesController : ControllerBase
{
    private readonly IResponseService _responseService;

    public ResponsesController(IResponseService responseService)
    {
        _responseService = responseService;
    }

    [HttpGet("by-recruitment")]
    public async Task<ActionResult<ApiResponse<List<ResponseDto>>>> GetResponsesByRecruitment([FromQuery] long recruitmentId)
    {
        var result = await _responseService.GetResponsesByRecruitmentAsync(recruitmentId);
        return Ok(ApiResponse<List<ResponseDto>>.Success(result));
    }

    [HttpGet("by-user")]
    public async Task<ActionResult<ApiResponse<List<ResponseDto>>>> GetResponsesByUser([FromQuery] long userId)
    {
        var result = await _responseService.GetResponsesByUserAsync(userId);
        return Ok(ApiResponse<List<ResponseDto>>.Success(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ResponseDto>>> CreateResponse([FromBody] CreateResponseRequest request)
    {
        try
        {
            var result = await _responseService.CreateResponseAsync(request.RecruitmentId, request.ResponserId);
            return Ok(ApiResponse<ResponseDto>.Success(result, "回应成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return Ok(ApiResponse<ResponseDto>.Fail(ex.Message, 404));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<ResponseDto>.Fail(ex.Message, 409));
        }
    }

    [HttpPost("delete")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteResponse([FromBody] DeleteResponseRequest request)
    {
        var result = await _responseService.DeleteResponseAsync(request.Id, request.Reason);
        return Ok(ApiResponse<bool>.Success(result, result ? "删除成功" : "回应不存在"));
    }

    [HttpPost("status")]
    public async Task<ActionResult<ApiResponse<ResponseDto>>> UpdateResponseStatus([FromBody] UpdateResponseStatusRequest request)
    {
        var result = await _responseService.UpdateResponseStatusAsync(request.Id, request.ResponseStatus);
        if (result == null)
            return Ok(ApiResponse<ResponseDto>.Fail("回应不存在", 404));
        return Ok(ApiResponse<ResponseDto>.Success(result, "更新成功"));
    }
}

public class CreateResponseRequest
{
    [JsonPropertyName("recruitment_id")]
    public long RecruitmentId { get; set; }

    [JsonPropertyName("responser_id")]
    public long ResponserId { get; set; }
}

public class DeleteResponseRequest
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class UpdateResponseStatusRequest
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("response_status")]
    public string ResponseStatus { get; set; } = string.Empty;
}
