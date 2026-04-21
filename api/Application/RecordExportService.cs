using EToolkit.Infrastructure;

namespace EToolkit.Application;


public sealed class RecordExportService : IRecordExportService
{
    private const int HardCap = 10000;

    private readonly ICsvRecordImporter _importer;
    private readonly IRecordFilteringService _filteringService;
    private readonly IRecordWriterService<CsvComponentPlacementRow> _exporter;

    public RecordExportService(
        ICsvRecordImporter importer,
        IRecordFilteringService filteringService,
        IRecordWriterService<CsvComponentPlacementRow> exporter)
    {
        _importer = importer;
        _filteringService = filteringService;
        _exporter = exporter;
    }

    public Task ExportAsync(Stream csvInput, Stream csvOutput, int? take, IReadOnlySet<string> acceptedOverrides, CancellationToken cancellation)
    {
        var rows = _importer.Import(csvInput);
        var classified = _filteringService.ClassifyRecords(rows);

        var exportable = classified.Where(r => r.Status == RowStatus.Accepted ||
                        (r.Status == RowStatus.Unknown &&
                         acceptedOverrides.Contains(r.Row.Name ?? string.Empty)))
            .Select(r => r.Row);

        var limit = take is null ? HardCap : Math.Clamp(take.Value, 1, HardCap);

        return _exporter.WriteAsync(exportable.Take(limit), csvOutput, cancellation);
    }
}