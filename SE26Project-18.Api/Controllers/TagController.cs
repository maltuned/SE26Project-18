using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tags")]
internal sealed class TagController : ControllerBase
{
    private readonly AppDbContext _db;

    public TagController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("games")]
    public async Task<ActionResult> GetGameTags(CancellationToken ct)
    {
        var tags = await _db.GameTags
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(ct);
        return Ok(tags);
    }

    [HttpGet("recruitment")]
    public async Task<ActionResult> GetRecruitmentTags(CancellationToken ct)
    {
        var tags = await _db.UserTags
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(ct);
        return Ok(tags);
    }
}
