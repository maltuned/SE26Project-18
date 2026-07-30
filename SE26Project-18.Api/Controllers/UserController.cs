using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Media;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public sealed class UserController : ControllerBase
{
    private readonly IUserService _userService;

    private readonly IMediaService _mediaService;

    public UserController(IUserService userService, IMediaService mediaService)
    {
        _userService = userService;
        _mediaService = mediaService;
    }

    [HttpGet("{id:long}/avatar")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvatar(long id, CancellationToken ct)
    {
        return ToFileResult(await _mediaService.OpenUserAvatarAsync(id, ct));
    }

    [HttpPut("me/avatar")]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> PutAvatar([FromForm] IFormFile file, CancellationToken ct)
    {
        await _mediaService.StoreUserAvatarAsync(GetCurrentUserId(), file, ct);
        return NoContent();
    }

    [HttpDelete("me/avatar")]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        await _mediaService.DeleteUserAvatarAsync(GetCurrentUserId(), ct);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        return Ok(
            await _userService.GetByIdAsync(GetCurrentUserId(), ct)
                ?? throw new NotFoundException("User not found.")
        );
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        return Ok(
            await _userService.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("User not found.")
        );
    }

    [HttpPatch("me")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateUserRequest request,
        CancellationToken ct
    )
    {
        var user = await _userService.UpdateAsync(GetCurrentUserId(), request, ct);
        return Ok(user);
    }

    [HttpPatch("{id:long}/suspension")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> SetSuspension(
        long id,
        [FromBody] SetUserSuspensionRequest request,
        CancellationToken ct
    )
    {
        return Ok(await _userService.SetSuspensionAsync(GetCurrentUserId(), id, request, ct));
    }

    private long GetCurrentUserId()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            throw new AuthenticationException("Token does not contain a valid user identifier.");
        }

        return userId;
    }

    private static IActionResult ToFileResult(MediaFile? media)
    {
        if (media is null)
        {
            return new NotFoundResult();
        }

        return new FileStreamResult(media.Stream, "image/webp")
        {
            EntityTag = media.EntityTag,
            LastModified = media.LastModified,
            EnableRangeProcessing = false,
        };
    }
}
