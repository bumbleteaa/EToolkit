using EToolkit.Infrastructure;

namespace EToolkit.Application;

// Use case for importing records from a CSV stream, applying filtering, and returning the filtered list of CsvComponentPlacementRow.
public class RecordImportUseCase : IRecordImportUseCase
{
    private readonly ICsvRecordImporter _importer;
    private readonly IRecordFilteringService _filter;

    public RecordImportUseCase(ICsvRecordImporter importer, IRecordFilteringService filter)
    {
        _importer = importer;
        _filter = filter;
    }

    public Task<List<CsvComponentPlacementRow>> ExecuteAsync(Stream csvStream)
    {
        var rows = _importer.Import(csvStream);
        var filtered = _filter.FilteredRecord(rows).ToList();

        return Task.FromResult(filtered);
    }
}
