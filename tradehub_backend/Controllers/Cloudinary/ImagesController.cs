using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Services;

namespace TradeHub.Controllers;

[ApiController]
[Route("api/images")]
[Authorize]
public class ImagesController : ControllerBase
{
    private const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private readonly IImageService _images;

    public ImagesController(IImageService images)
    {
        _images = images;
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileBytes + 1_048_576)] // file + 1 MB for multipart overhead
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No image was uploaded.");

        if (file.Length > MaxFileBytes)
            return BadRequest("Image is too large (max 10 MB).");

        if (!AllowedTypes.Contains(file.ContentType))
            return BadRequest("Only JPEG, PNG or WebP images are allowed.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            await using var stream = file.OpenReadStream();
            var url = await _images.UploadAsync(stream, file.FileName, $"tradehub/{userId}");
            return Ok(new { url });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, ex.Message);
        }
    }
}