using System.Runtime.InteropServices;
using EToolkit.Application;
using Microsoft.AspNetCore.Mvc;

namespace EToolkit.Controller;

[ApiController]
[Route("api/placement")]
public class PlacementController : ControllerBase
{
    private readonly RecordImportService _importService;
    private readonly RecordExportService _exportService;

    public PlacementController(RecordImportService importService, RecordExportService exportService)
    {
        _importService = importService;
        _exportService = exportService;
    }

    [HttpPost("import")]
    public IActionResult Import([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Csv Required");

        try
        {
            using var stream = file.OpenReadStream();
            var placements = _importService.Import(stream);

            return Ok(new
            {
                Count = placements.Count,
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("filter-preview")]
    public IActionResult FilterPreview(
        [FromForm] IFormFile file,
        [FromQuery] int? take,
        [FromServices] RecordFilterPreviewService service)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Csv Required");

        using var stream = file.OpenReadStream();
        var filtered = service.Preview(stream, take);

        return Ok(new
        {
            Count = filtered.TotalCount,
            Truncated = filtered.IsTruncated,
            Data = filtered.Rows
        });
    }

    [HttpPost("export")]
    public async Task<IActionResult> Export(
        [FromForm] IFormFile file,
        [FromForm] int? take, CancellationToken cancellation)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Csv Required");

        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = "attachment; filename=\"workingfile.csv\"";

        await using var input = file.OpenReadStream();
        await _exportService.ExportAsync(input, Response.Body, take, cancellation);

        return new EmptyResult();
    }
}