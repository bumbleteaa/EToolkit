using EToolkit.Infrastructure;

namespace EToolkit.Application;

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
        var filtered = _filter.FilterByFootprint(rows).ToList();
        
        return Task.FromResult(filtered);
    }
}
