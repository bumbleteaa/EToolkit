using EToolkit.Infrastructure;

namespace EToolkit.Application;

// Use case for importing records from a CSV stream, applying filtering, and returning the filtered list of CsvComponentPlacementRow.
public class RecordImportService : IRecordImportService
{
    private readonly ICsvRecordImporter _importer;

    public RecordImportService(ICsvRecordImporter importer)
    {
        _importer = importer;
    }

    public CsvComponentPlacementRow[] Import(Stream csvStream)
    {
        return _importer.Import(csvStream).ToArray();
    }
}
