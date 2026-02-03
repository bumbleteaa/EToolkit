using EToolkit.Infrastructure;

namespace EToolkit.Application;

public interface IRecordFilteringService
{
        IEnumerable<CsvComponentPlacementRow> FilteredRecord(IEnumerable<CsvComponentPlacementRow> rows);
}