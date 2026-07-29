using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;

    public ImageController(IImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<ApiResponse<string>>> Upload(IFormFile file, [FromForm] string folder = "general", [FromForm] string? name = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("请选择文件"));

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(ApiResponse<string>.Fail("仅支持 jpg、png、gif、webp 格式"));

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(ApiResponse<string>.Fail("文件大小不能超过 5MB"));

        var allowedFolders = new[] { "avatars", "covers", "icons", "general" };
        if (!allowedFolders.Contains(folder))
            folder = "general";

        using var stream = file.OpenReadStream();
        var objectName = !string.IsNullOrEmpty(name)
            ? await _imageService.UploadWithNameAsync(stream, $"{folder}/{name}{extension}", file.ContentType)
            : await _imageService.UploadAsync(stream, file.FileName, file.ContentType, folder);
        var url = $"/Image/file/{objectName}";

        return Ok(ApiResponse<string>.Success(url, "上传成功"));
    }

    [HttpPost("upload-avatar")]
    public async Task<ActionResult<ApiResponse<string>>> UploadAvatar(IFormFile file, [FromForm] long userId)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("请选择文件"));

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(ApiResponse<string>.Fail("仅支持 jpg、png、gif、webp 格式"));

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(ApiResponse<string>.Fail("文件大小不能超过 5MB"));

        if (userId <= 0)
            return BadRequest(ApiResponse<string>.Fail("无效的用户ID"));

        // 删除旧头像
        await _imageService.DeleteByPrefixAsync($"avatars/{userId}.");

        using var stream = file.OpenReadStream();
        var objectName = $"avatars/{userId}{extension}";
        await _imageService.UploadWithNameAsync(stream, objectName, file.ContentType);
        var url = $"/Image/file/{objectName}";

        return Ok(ApiResponse<string>.Success(url, "上传成功"));
    }

    [HttpGet("file/{**objectName}")]
    public async Task<IActionResult> GetFile(string objectName)
    {
        var stream = await _imageService.GetStreamAsync(objectName);
        var contentType = objectName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
            : objectName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ? "image/gif"
            : objectName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp"
            : "image/jpeg";
        return File(stream, contentType);
    }
}