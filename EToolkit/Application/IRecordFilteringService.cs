using EToolkit.Infrastructure;

namespace EToolkit.Application;

public interface IRecordFilteringService
{
    IEnumerable<CsvComponentPlacementRow> FilteredRecord(IEnumerable<CsvComponentPlacementRow> rows);

    IEnumerable<AnnotatedRow> ClassifyRecords(IEnumerable<CsvComponentPlacementRow> rows);
}