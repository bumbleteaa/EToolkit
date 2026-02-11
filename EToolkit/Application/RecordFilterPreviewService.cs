using EToolkit.Infrastructure;

namespace EToolkit.Application;

public class RecordFilterPreviewService
{
    private const int HardCap = 1000;
    private readonly ICsvRecordImporter _recordImporter;
    private readonly IRecordFilteringService _recordFilter;

    public RecordFilterPreviewService(ICsvRecordImporter recordImporter, IRecordFilteringService recordFilter)
    {
        _recordImporter = recordImporter;
        _recordFilter = recordFilter;
    }

    public PreviewResult Preview(Stream csvStream, int? take, bool includeTotalCount = true)
    {
        var limit = ResolveLimit(take);
        var rows = _recordImporter.Import(csvStream);
        var filtered = _recordFilter.FilteredRecord(rows);
        
        var capacity = limit <= 10_000 ? limit : 10_000;
        var data = new List<CsvComponentPlacementRow>(capacity);
        
        var total = 0;

        foreach (var r in filtered)
        {
            total++;
            if (data.Count < limit) data.Add(r);
            if (!includeTotalCount && data.Count >= limit)
                break;
        }
        
        var effectiveTotal = includeTotalCount ? total : data.Count;
        var truncated = includeTotalCount ? data.Count < total : false;

        return new PreviewResult(effectiveTotal, data, truncated, limit);
    }

    private static int ResolveLimit(int? take)
    {
        if (take is null) return HardCap;
        
        if (take.Value <= 0) 
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be > 0");
        
        return Math.Min(take.Value, HardCap);
    }

    public sealed record PreviewResult(
        int TotalCount,
        IReadOnlyList<CsvComponentPlacementRow> Rows,
        bool IsTruncated, 
        int LimitApplied
    );
}