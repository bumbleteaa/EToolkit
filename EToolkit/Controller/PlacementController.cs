using EToolkit.Application;
using Microsoft.AspNetCore.Mvc;

namespace EToolkit.Controller;

[ApiController]
[Route("api/placement")]
public class PlacementController : ControllerBase
{
    private readonly IRecordImportService _importService;
    private readonly IRecordExportService _exportService;

    public PlacementController(IRecordImportService importService, IRecordExportService exportService)
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
            var placement = _importService.Import(stream);
            return Ok(new { Count = placement.Count });
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
        var result = service.Preview(stream, take);

        return Ok(result);
    }

    [HttpPost("export")]
    public async Task<IActionResult> Export(
        [FromForm] IFormFile file,
        [FromForm] int? take,
        // Daftar designator (kolom Name) yang operator putuskan untuk di-accept secara manual
        // dari status Unknown. Dikirim sebagai comma-separated string dari frontend.
        // Contoh: "R1,C4,U2" — baris dengan Name tersebut akan masuk ke output CSV
        // meskipun footprintnya tidak dikenali sistem, selama bukan Rejected.
        [FromForm] string? acceptedOverrides,
        CancellationToken cancellation)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Csv Required");

        // Parse override list — empty string atau null berarti tidak ada override,
        // export hanya mengambil Accepted murni.
        var overrideSet = string.IsNullOrWhiteSpace(acceptedOverrides)
            ? (IReadOnlySet<string>)new HashSet<string>()
            : acceptedOverrides
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.Ordinal);

        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = "attachment; filename=\"workingfile.csv\"";

        await using var input = file.OpenReadStream();
        await _exportService.ExportAsync(input, Response.Body, take, overrideSet, cancellation);

        return new EmptyResult();
    }
}