using EToolkit.Infrastructure;

namespace EToolkit.Application;

// Use case for importing records from a CSV stream, applying filtering, and returning the filtered list of CsvComponentPlacementRow.
public class RecordImportService : IRecordImportService
{
    private readonly ICsvRecordImporter _importer;
    private readonly IRecordFilteringService _filter;

    public RecordImportService(ICsvRecordImporter importer, IRecordFilteringService filter)
    {
        _importer = importer;
        _filter = filter;
    }

    public List<CsvComponentPlacementRow> Import(Stream csvStream)
    {
        var rows = _importer.Import(csvStream);
        return _filter.FilteredRecord(rows).ToList();
    }
}
