using EToolkit.Application;
using EToolkit.Controllers;
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
            var rows = _importService.Import(stream);

            return Ok(rows);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
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

        //Flatten annotated row for frontend
        var flatRows = result.Data.Rows.Select((r, i) => new FilterPreviewRowDto(RowIndex: i + 1,
            Status: r.Status.ToString(),
            Comp: r.Row.comp ?? "",
            Name: r.Row.Name ?? "",
            Value: r.Row.Value ?? "",
            Footprint: r.Row.Footprint ?? "",
            Desc: r.Row.Desc ?? "",
            Side: r.Row.Side ?? "",
            Issues: r.RejectCode is null ? [] : [r.RejectCode])).ToArray();

        var dataDto = new FilterPreviewDataDto(
            TotalCount: result.Data.TotalCount,
            Rows: flatRows,
            IsTruncated: result.Data.IsTruncated,
            LimitApplied: result.Data.LimitApplied
        );

        return Ok(new PipelineResponse<FilterPreviewDataDto>(dataDto, result.Report));
    }

    [HttpPost("export")]
    public async Task<IActionResult> Export(
        [FromForm] IFormFile file,
        [FromForm] int? take,

        [FromForm] string? acceptedOverrides,
        CancellationToken cancellation)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Csv Required");
        var overrideSet = string.IsNullOrWhiteSpace(acceptedOverrides)
            ? (IReadOnlySet<string>)new HashSet<string>()
            : acceptedOverrides
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.Ordinal);

        using var buffer = new MemoryStream();
        await using var input = file.OpenReadStream();
        await _exportService.ExportAsync(input, buffer, take, overrideSet, cancellation);

        if (buffer.Length == 0)
            return NoContent(); // 204

        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = "attachment; filename=\"_filtered.csv\"";

        buffer.Seek(0, SeekOrigin.Begin);
        await buffer.CopyToAsync(Response.Body, cancellation);

        return new EmptyResult();
    }
}