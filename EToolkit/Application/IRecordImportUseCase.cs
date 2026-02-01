using EToolkit.Infrastructure;

namespace EToolkit.Application;

public interface IRecordImportUseCase
{
    Task<List<CsvComponentPlacementRow>> ExecuteAsync(Stream csvStream);
}