using EToolkit.Infrastructure;

namespace EToolkit.Application;

public interface IRecordImportService
{
    List<CsvComponentPlacementRow> Import(Stream csvStream);
}