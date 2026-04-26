using EToolkit.Infrastructure;

namespace EToolkit.Application;

/// <summary>
/// Bertanggung jawab atas satu hal: mengklasifikasikan setiap row dan memutuskan
/// apakah row tersebut lolos filtering. Cara melaporkan keputusan itu sepenuhnya
/// didelegasikan ke IRecordIssueCollector — service ini tidak tahu soal logging.
/// </summary>
public sealed class RecordFilteringService : IRecordFilteringService
{
    private readonly IFootprintNormalizer _normalizer;
    private readonly IRecordIssueCollector _collector;
    private readonly ILogger<RecordFilteringService> _logger;

    //Mappung each Normalized kind to classificaton decision
    private static readonly IReadOnlyDictionary<NormalizedKind, Func<CsvComponentPlacementRow, NormalizedFootprint, AnnotatedRow>>
    KindStrategies = new Dictionary<NormalizedKind, Func<CsvComponentPlacementRow, NormalizedFootprint, AnnotatedRow>>
    {
        [NormalizedKind.StandardPackage] = (row, n) => Accept(row, n),
        [NormalizedKind.GenericFootprint] = (row, n) => Warn(row, "GENERIC_FOOTPRINT", n),
        [NormalizedKind.Unknown] = (row, n) => Warn(row, "UNKNOWN_FOOTPRINT", n),
        [NormalizedKind.NonPlaceable] = (row, n) => Reject(row, "NON_PLACEABLE", n),
    };

    public RecordFilteringService(IFootprintNormalizer normalizer, IRecordIssueCollector collector, ILogger<RecordFilteringService> logger)
    {
        _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
        _logger = logger ?? throw new ArgumentException(nameof(logger));
    }

    // Thin wrapper — export dan import hanya perlu yang benar-benar Accepted.
    public IEnumerable<CsvComponentPlacementRow> FilteredRecord(IEnumerable<CsvComponentPlacementRow> rows)
        => ClassifyRecords(rows)
            .Where(r => r.Status == RowStatus.Accepted)
            .Select(r => r.Row);

    // AnnotatedRow is the result row container, have a status of row instead of raw data
    public IEnumerable<AnnotatedRow> ClassifyRecords(IEnumerable<CsvComponentPlacementRow> rows)
    {
        foreach (var (row, index) in rows.Select((r, i) => (r, i + 1)))
        {
            var annotated = Classify(row);

            // TP-2: emitted here because index context lives here, not inside Classify().
            // We log after Classify() so we have access to the NormalizedFootprint result.
            if (annotated.Normalized is { } n)
                _logger.LogDebug(
                    "[TP-2] #{RowIndex} Normalizer: Raw='{Raw}' → Canonical='{Canonical}', Kind='{Kind}', Family='{Family}', Name='{Name}'",
                    index, n.Raw, n.Canonical, n.Kind, n.Family ?? "—", row.Name);

            _collector.Report(annotated);
            yield return annotated;
        }
    }

    // Classify() adalah satu-satunya tempat logika klasifikasi hidup.
    // Tidak ada efek samping di sini — murni input → output.
    private AnnotatedRow Classify(CsvComponentPlacementRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Footprint))
            return Reject(row, "EMPTY_VALUE", null);

        if (ContainsDnp(row.Value) || ContainsDnp(row.Desc) ||
            ContainsDnp(row.Footprint) || ContainsDnp(row.Name))
            return Reject(row, "DNP", null);

        var n = _normalizer.NormalizeFootprint(row.Footprint);

        return KindStrategies.TryGetValue(n.Kind, out var strategy)
            ? strategy(row, n)
            : Reject(row, "UNHANDLED_KIND", n);
    }

    private static AnnotatedRow Accept(CsvComponentPlacementRow row, NormalizedFootprint n)
        => new(row, RowStatus.Accepted, null, n);

    private static AnnotatedRow Warn(CsvComponentPlacementRow row, string code, NormalizedFootprint n)
        => new(row, RowStatus.Unknown, code, n);

    private static AnnotatedRow Reject(CsvComponentPlacementRow row, string code, NormalizedFootprint? n)
        => new(row, RowStatus.Rejected, code, n);

    private static bool ContainsDnp(string? s) =>
        !string.IsNullOrWhiteSpace(s) &&
        s.Trim().Equals("DNP", StringComparison.OrdinalIgnoreCase);
}