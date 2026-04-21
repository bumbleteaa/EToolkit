using EToolkit.Infrastructure;

namespace EToolkit.Application;

/// <summary>
/// Orchestrates CSV import, row classification, and report generation into a single preview pipeline result.
/// </summary>
public class RecordFilterPreviewService
{
    private const int HardCap = 1000;
    private const int MaxIssues = 50;

    private readonly ICsvRecordImporter _recordImporter;
    private readonly IRecordFilteringService _recordFilter;

    // Class constructor
    public RecordFilterPreviewService(
        ICsvRecordImporter recordImporter,
        IRecordFilteringService recordFilter)
    {
        _recordImporter = recordImporter;
        _recordFilter = recordFilter;
    }

    /// <summary>
    /// Classifies all rows from the CSV stream, collects unknown footprint aggregates, and returns paginated data with a diagnostic report.
    /// </summary>
    public PipelineResponse<PreviewResult> Preview(Stream csvStream, int? take, bool includeTotalCount = true)
    {
        var limit = ResolveLimit(take);
        var rows = _recordImporter.Import(csvStream);
        var classified = _recordFilter.ClassifyRecords(rows); // semua baris, bukan hanya Accepted

        var capacity = Math.Min(limit, 10_000);
        var data = new List<AnnotatedRow>(capacity);

        var total = 0;
        var unknown = new Dictionary<string, UnknownAgg>(StringComparer.OrdinalIgnoreCase);

        // Iterate all classified rows, filling the page buffer and aggregating unknown footprints by normalized key.
        foreach (var annotated in classified)
        {
            total++;
            if (data.Count < limit) data.Add(annotated);

            // Track unknown-status rows that carry a normalized footprint for aggregation.
            if (annotated.Status == RowStatus.Unknown && annotated.Normalized is { } n)
            {
                var key = string.IsNullOrEmpty(n.Key) ? "(EMPTY)" : n.Key;

                // Initialize a new aggregate entry if this footprint key has not been seen before.
                if (!unknown.TryGetValue(key, out var agg))
                    agg = new UnknownAgg(
                        Key: key,
                        SampleRaw: n.Raw,
                        SampleName: annotated.Row.Name,
                        SampleSide: annotated.Row.Side,
                        SampleRowNumber: total,
                        Count: 0
                    );

                unknown[key] = agg with { Count = agg.Count + 1 }; // Increment occurrence count for this footprint key.
            }

            if (!includeTotalCount && data.Count >= limit) break;
        }

        var effectiveTotal = includeTotalCount ? total : data.Count;
        var truncated = includeTotalCount && data.Count < total;

        var previewResult = new PreviewResult(effectiveTotal, data, truncated, limit);

        // Convert the top unknown aggregates (by frequency) into structured pipeline issues for the report matters.
        var issues = unknown.Values
            .OrderByDescending(x => x.Count)
            .Take(MaxIssues)
            .Select(x => new PipelineIssue(
                Code: "UNKNOWN_FOOTPRINT",
                Severity: Severity.Warning,
                Message: $"Footprint '{x.SampleRaw}' (key: '{x.Key}') needs review. " +
                         $"Sample at row {x.SampleRowNumber}, name '{x.SampleName}', side '{x.SampleSide}'. Total: {x.Count}.",
                Context: new IssueContext(
                    FootprintRaw: x.SampleRaw,
                    FootprintKey: x.Key,
                    Name: x.SampleName,
                    Side: x.SampleSide,
                    RowNumber: x.SampleRowNumber,
                    Count: x.Count
                )
            ))
            .ToArray();

        // Build the diagnostic report, marking export as ready only when no unknown footprints remain.
        var report = new PipelineReport(
            Stage: "filter-preview",
            IsExportReady: unknown.Count == 0,
            RulesetVersion: null,
            Issues: issues
        );

        return new PipelineResponse<PreviewResult>(previewResult, report);  // Wrap paginated data and diagnostic report into a single pipeline response.
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
        IReadOnlyList<AnnotatedRow> Rows,
        bool IsTruncated,
        int LimitApplied
    );

    private sealed record UnknownAgg(
        string Key,
        string SampleRaw,
        string? SampleName,
        string? SampleSide,
        int? SampleRowNumber,
        int Count
    );
}