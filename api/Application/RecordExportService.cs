using EToolkit.Infrastructure;

namespace EToolkit.Application;


public sealed class RecordExportService : IRecordExportService
{
    private const int HardCap = 10000;

    private readonly ICsvRecordImporter _importer;
    private readonly IRecordFilteringService _filteringService;
    private readonly IRecordWriterService<CsvComponentPlacementRow> _exporter;
    private readonly ILogger<RecordExportService> _logger;

    public RecordExportService(
        ICsvRecordImporter importer,
        IRecordFilteringService filteringService,
        IRecordWriterService<CsvComponentPlacementRow> exporter, ILogger<RecordExportService> logger)
    {
        _importer = importer;
        _filteringService = filteringService;
        _exporter = exporter;
        _logger = logger;
    }

    public async Task ExportAsync(Stream csvInput, Stream csvOutput, int? take, IReadOnlySet<string> acceptedOverrides, CancellationToken cancellation)
    {
        var rows = _importer.Import(csvInput);
        var classified = _filteringService.ClassifyRecords(rows);

        var accepted = new List<CsvComponentPlacementRow>();
        var overridden = new List<CsvComponentPlacementRow>();

        foreach (var r in classified)
        {
            if (r.Status == RowStatus.Accepted)
                accepted.Add(r.Row);
            else if (r.Status == RowStatus.Unknown &&
                     acceptedOverrides.Contains(r.Row.Name ?? string.Empty))
                overridden.Add(r.Row);
        }

        var limit = take is null ? HardCap : Math.Clamp(take.Value, 1, HardCap);
        var exportable = accepted.Concat(overridden).Take(limit);

        // TP-5: surface exactly how many rows exit the pipeline and via which path.
        // If overridden > 0, an operator manually approved Unknown rows — this is intentional
        // and should be visible in the audit trail.
        _logger.LogInformation(
            "[TP-5] Export: {Accepted} accepted + {Overridden} overridden = {Total} rows written (limit: {Limit})",
            accepted.Count, overridden.Count, Math.Min(accepted.Count + overridden.Count, limit), limit);

        await _exporter.WriteAsync(exportable, csvOutput, cancellation);
    }
}