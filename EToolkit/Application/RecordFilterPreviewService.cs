using EToolkit.Infrastructure;

namespace EToolkit.Application;

public class RecordFilterPreviewService
{
    private readonly ICsvRecordImporter _recordImporter;
    private readonly IRecordFilteringService _recordFilter;

    public RecordFilterPreviewService(ICsvRecordImporter recordImporter, IRecordFilteringService recordFilter)
    {
        _recordImporter = recordImporter;
        _recordFilter = recordFilter;
    }

    public IReadOnlyList<CsvComponentPlacementRow> Preview(Stream csvStream, int take = 280)
    {
        var rows = _recordImporter.Import(csvStream);
        return _recordFilter.FilteredRecord(rows).Take(take).ToList();
    }
}