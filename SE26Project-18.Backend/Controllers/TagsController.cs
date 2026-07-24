using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class GameTagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public GameTagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<GameTagDto>>>> GetGameTags()
    {
        var tags = await _tagService.GetGameTagsAsync();
        return Ok(ApiResponse<List<GameTagDto>>.Success(tags));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GameTagDto>>> CreateGameTag([FromBody] CreateTagRequest request)
    {
        var tag = await _tagService.CreateGameTagAsync(request.Name);
        return Ok(ApiResponse<GameTagDto>.Success(tag));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<GameTagDto>>> UpdateGameTag(long id, [FromBody] CreateTagRequest request)
    {
        var tag = await _tagService.UpdateGameTagAsync(id, request.Name);
        if (tag == null)
            return Ok(ApiResponse<GameTagDto>.Fail("标签不存在", 404));
        return Ok(ApiResponse<GameTagDto>.Success(tag));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteGameTag(long id)
    {
        var result = await _tagService.DeleteGameTagAsync(id);
        return Ok(ApiResponse<bool>.Success(result, result ? "删除成功" : "标签不存在"));
    }
}

[ApiController]
[Route("[controller]")]
public class RecruitmentTagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public RecruitmentTagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RecruitmentTagDto>>>> GetRecruitmentTags()
    {
        var tags = await _tagService.GetRecruitmentTagsAsync();
        return Ok(ApiResponse<List<RecruitmentTagDto>>.Success(tags));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RecruitmentTagDto>>> CreateRecruitmentTag([FromBody] CreateTagRequest request)
    {
        var tag = await _tagService.CreateRecruitmentTagAsync(request.Name);
        return Ok(ApiResponse<RecruitmentTagDto>.Success(tag));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<RecruitmentTagDto>>> UpdateRecruitmentTag(long id, [FromBody] CreateTagRequest request)
    {
        var tag = await _tagService.UpdateRecruitmentTagAsync(id, request.Name);
        if (tag == null)
            return Ok(ApiResponse<RecruitmentTagDto>.Fail("标签不存在", 404));
        return Ok(ApiResponse<RecruitmentTagDto>.Success(tag));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRecruitmentTag(long id)
    {
        var result = await _tagService.DeleteRecruitmentTagAsync(id);
        return Ok(ApiResponse<bool>.Success(result, result ? "删除成功" : "标签不存在"));
    }
}

public class CreateTagRequest
{
    public string Name { get; set; } = string.Empty;
}
