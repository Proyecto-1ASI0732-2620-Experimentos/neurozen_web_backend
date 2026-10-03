using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;

namespace neurozen.API.Wellness.Interfaces.REST;

[ApiController]
[Route("api/v1/contents")]
public class ContentsController(AppDbContext context) : ControllerBase
{
    [HttpGet("meditations")]
    public async Task<IActionResult> GetMeditations()
    {
        var meditations = await context.Set<Domain.Entities.Meditation>()
            .AsNoTracking()
            .Select(meditation => new
            {
                id = meditation.Id,
                title = meditation.Title,
                description = meditation.Description,
                durationMinutes = meditation.DurationMinutes,
                imageUrl = meditation.ImageUrl,
                audioUrl = meditation.AudioUrl
            })
            .ToListAsync();

        return Ok(meditations);
    }
}