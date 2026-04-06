using EToolkit.Infrastructure;
using Swashbuckle.AspNetCore.Swagger;

namespace EToolkit.Application;

public class RecordFilterPreviewService
{
    private const int HardCap = 1000;
    private const int MaxIssues = 50;
    private readonly ICsvRecordImporter _recordImporter;
    private readonly IRecordFilteringService _recordFilter;
    private readonly FootprintNormalizer _footprintNormalizer;

    public RecordFilterPreviewService(ICsvRecordImporter recordImporter, IRecordFilteringService recordFilter, FootprintNormalizer footprintNormalizer)
    {
        _recordImporter = recordImporter;
        _recordFilter = recordFilter;
        _footprintNormalizer = footprintNormalizer;
    }

    public PipelineResponse<PreviewResult> Preview(Stream csvStream, int? take, bool includeTotalCount = true)
    {
        var limit = ResolveLimit(take);
        var rows = _recordImporter.Import(csvStream);
        var filtered = _recordFilter.FilteredRecord(rows);

        var capacity = limit <= 10_000 ? limit : 10_000;
        var data = new List<CsvComponentPlacementRow>(capacity);

        var total = 0;

        var unknown = new Dictionary<string, UnknownAgg>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in filtered)
        {
            total++;
            if (data.Count < limit) data.Add(r);

            var rawFootprint = r.Footprint ?? string.Empty;
            var normalizedFootprint = _footprintNormalizer.NormalizeFootprint(rawFootprint);

            if (normalizedFootprint.Kind == FootprintNormalizer.NormalizedKind.Unknown)
            {
                var key = string.IsNullOrEmpty(normalizedFootprint.Key) ? "(EMPTY)" : normalizedFootprint.Key;

                if (!unknown.TryGetValue(key, out var agg))
                {
                    agg = new UnknownAgg(
                        Key: key,
                        SampleRaw: normalizedFootprint.Raw,
                        SampleDesignator: r.Name,
                        SampleSide: r.Side,
                        SampleRowNumber: total,
                        Count: 0
                    );

                    unknown[key] = agg with { Count = agg.Count + 1 };
                }
            }
            //If 
            if (!includeTotalCount && data.Count >= limit)
                break;
        }

        var effectiveTotal = includeTotalCount ? total : data.Count;
        var truncated = includeTotalCount ? data.Count < total : false;

        var previewResult = new PreviewResult(
            effectiveTotal,
            data,
            truncated,
            limit
        );

        var issues = unknown.Values
            .OrderByDescending(x => x.Count)
            .Take(MaxIssues)
            .Select(x => new PipelineIssue(
                Code: "UNKNOWN_FOOTPRINT",
                Severity: Severity.Warning,
                Message: $"Footprint '{x.SampleRaw}' (key: '{x.Key}') is unknown. Sample occurrence at row {x.SampleRowNumber} with designator '{x.SampleDesignator}' on side '{x.SampleSide}'. Total occurrences: {x.Count}.",
                Context: new IssueContext(
                    FootprintRaw: x.SampleRaw,
                    FootprintKey: x.Key,
                    Designator: x.SampleDesignator,
                    Side: x.SampleSide,
                    RowNumber: x.SampleRowNumber,
                    Count: x.Count
                )
            ))
            .ToArray();

        var isExportReady = unknown.Count == 0;

        // If there are unknown footprints, we consider the export not ready, as it indicates potential issues with the data that may need to be addressed before exporting. The presence of unknown footprints can lead to incorrect filtering and grouping in the final output, so it's important to surface these issues in the preview stage to allow for corrective actions.
        var report = new PipelineReport(
            Stage: "filter-preview",
            IsExportReady: isExportReady,
            RulesetVersion: null,
            Issues: issues
        );

        return new PipelineResponse<PreviewResult>(previewResult, report);
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

    private sealed record UnknownAgg(
        string Key,
        string SampleRaw,
        string? SampleDesignator,
        string? SampleSide,
        int? SampleRowNumber,
        int Count
    );
}