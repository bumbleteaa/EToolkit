using EToolkit.Infrastructure;

namespace EToolkit.Application;

public interface IRecordImportService
{
    CsvComponentPlacementRow[] Import(Stream csvStream);
}