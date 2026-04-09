using EToolkit.Infrastructure;

namespace EToolkit.Application;

public class RecordFilterPreviewService
{
    private const int HardCap = 1000;
    private const int MaxIssues = 50;

    private readonly ICsvRecordImporter _recordImporter;
    private readonly IRecordFilteringService _recordFilter;
    // IFootprintNormalizer tidak lagi diperlukan di sini — hasil normalisasi
    // sudah ada di AnnotatedRow.Normalized, tidak perlu normalisasi ulang.

    public RecordFilterPreviewService(
        ICsvRecordImporter recordImporter,
        IRecordFilteringService recordFilter)
    {
        _recordImporter = recordImporter;
        _recordFilter = recordFilter;
    }

    public PipelineResponse<PreviewResult> Preview(Stream csvStream, int? take, bool includeTotalCount = true)
    {
        var limit = ResolveLimit(take);
        var rows = _recordImporter.Import(csvStream);
        var classified = _recordFilter.ClassifyRecords(rows); // semua baris, bukan hanya Accepted

        var capacity = Math.Min(limit, 10_000);
        var data = new List<AnnotatedRow>(capacity);

        var total = 0;
        var unknown = new Dictionary<string, UnknownAgg>(StringComparer.OrdinalIgnoreCase);

        foreach (var annotated in classified)
        {
            total++;
            if (data.Count < limit) data.Add(annotated);

            // Kita kumpulkan aggregasi hanya untuk Unknown — keduanya GENERIC dan UNKNOWN_FOOTPRINT
            // adalah kandidat untuk dilaporkan ke operator sebagai item yang perlu ditinjau.
            // Bug fix dari versi lama: sebelumnya preview mencari Unknown dari filtered rows
            // (hanya Accepted), sehingga tidak mungkin ditemukan. Sekarang kita iterasi semua.
            if (annotated.Status == RowStatus.Unknown && annotated.Normalized is { } n)
            {
                var key = string.IsNullOrEmpty(n.Key) ? "(EMPTY)" : n.Key;

                if (!unknown.TryGetValue(key, out var agg))
                    agg = new UnknownAgg(
                        Key: key,
                        SampleRaw: n.Raw,
                        SampleDesignator: annotated.Row.Name,
                        SampleSide: annotated.Row.Side,
                        SampleRowNumber: total,
                        Count: 0
                    );

                unknown[key] = agg with { Count = agg.Count + 1 };
            }

            if (!includeTotalCount && data.Count >= limit) break;
        }

        var effectiveTotal = includeTotalCount ? total : data.Count;
        var truncated = includeTotalCount && data.Count < total;

        var previewResult = new PreviewResult(effectiveTotal, data, truncated, limit);

        // Issues report mencakup semua Unknown — operator bisa melihat footprint mana
        // yang paling sering muncul dan perlu diprioritaskan untuk iterasi database.
        var issues = unknown.Values
            .OrderByDescending(x => x.Count)
            .Take(MaxIssues)
            .Select(x => new PipelineIssue(
                Code: "UNKNOWN_FOOTPRINT",
                Severity: Severity.Warning,
                Message: $"Footprint '{x.SampleRaw}' (key: '{x.Key}') needs review. " +
                         $"Sample at row {x.SampleRowNumber}, designator '{x.SampleDesignator}', side '{x.SampleSide}'. Total: {x.Count}.",
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

        var report = new PipelineReport(
            Stage: "filter-preview",
            IsExportReady: unknown.Count == 0,
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

    // Rows sekarang List<AnnotatedRow> — frontend mendapat status + RejectCode tiap baris
    // untuk keperluan color coding: hijau (Accepted), kuning (Unknown), merah (Rejected).
    public sealed record PreviewResult(
        int TotalCount,
        IReadOnlyList<AnnotatedRow> Rows,
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