using EToolkit.Infrastructure;

namespace EToolkit.Application;

public sealed class RecordFilteringService : IRecordFilteringService
{
    private readonly ILogger<RecordFilteringService> _logger;
    private readonly IFootprintNormalizer _normalizer;

    // Scoped service — HashSet ini hidup satu request, cukup untuk dedup log.
    private readonly HashSet<string> _loggedUnknown = new(StringComparer.Ordinal);
    private readonly HashSet<string> _loggedRejected = new(StringComparer.Ordinal);

    public RecordFilteringService(ILogger<RecordFilteringService> logger, IFootprintNormalizer normalizer)
    {
        _logger = logger;
        _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
    }

    // Thin wrapper — export hanya perlu yang benar-benar Accepted.
    public IEnumerable<CsvComponentPlacementRow> FilteredRecord(IEnumerable<CsvComponentPlacementRow> rows)
        => ClassifyRecords(rows)
            .Where(r => r.Status == RowStatus.Accepted)
            .Select(r => r.Row);

    public IEnumerable<AnnotatedRow> ClassifyRecords(IEnumerable<CsvComponentPlacementRow> rows)
    {
        foreach (var row in rows)
        {
            var annotated = Classify(row);
            LogAnnotated(annotated);
            yield return annotated;
        }
    }

    // Classify() adalah satu-satunya tempat logika klasifikasi hidup.
    // Tidak ada efek samping di sini — logging dipisah ke LogAnnotated().
    private AnnotatedRow Classify(CsvComponentPlacementRow row)
    {
        // Hard reject sebelum normalisasi — tidak ada path lain yang bisa meloloskan ini.
        if (string.IsNullOrWhiteSpace(row.Footprint))
            return Reject(row, "EMPTY_VALUE", null);

        if (ContainsDnp(row.Value) || ContainsDnp(row.Desc) ||
            ContainsDnp(row.Footprint) || ContainsDnp(row.Name))
            return Reject(row, "DNP", null);

        var n = _normalizer.NormalizeFootprint(row.Footprint);

        return n.Kind switch
        {
            // Satu-satunya jalan masuk ke Accepted — PassiveComponent atau PlaceableFamiliesComponent.
            NormalizedKind.StandardPackage => Accept(row, n),

            // Keduanya Unknown dari perspektif bisnis: footprint dikenali prefixnya
            // (GenericFootprint) atau tidak dikenali sama sekali (Unknown). Keduanya
            // adalah sinyal untuk iterasi database, bukan hard reject.
            NormalizedKind.GenericFootprint => Warn(row, "GENERIC_FOOTPRINT", n),
            NormalizedKind.Unknown => Warn(row, "UNKNOWN_FOOTPRINT", n),

            // Hard reject — mesin tidak bisa memasang komponen jenis ini.
            NormalizedKind.NonPlaceable => Reject(row, "NON_PLACEABLE", n),

            // Fallback defensif untuk NormalizedKind yang mungkin ditambah di masa depan.
            _ => Reject(row, "UNHANDLED_KIND", n),
        };
    }

    private static AnnotatedRow Accept(CsvComponentPlacementRow row, NormalizedFootprint n)
        => new(row, RowStatus.Accepted, null, n);

    private static AnnotatedRow Warn(CsvComponentPlacementRow row, string code, NormalizedFootprint n)
        => new(row, RowStatus.Unknown, code, n);

    private static AnnotatedRow Reject(CsvComponentPlacementRow row, string code, NormalizedFootprint? n)
        => new(row, RowStatus.Rejected, code, n);

    // Semua efek samping logging dikumpulkan di sini, terpisah dari logika klasifikasi.
    // Dedup per canonical supaya log tidak banjir saat ada ratusan baris dengan footprint sama.
    private void LogAnnotated(AnnotatedRow annotated)
    {
        var row = annotated.Row;
        var n = annotated.Normalized;
        var code = annotated.RejectCode ?? "UNKNOWN";
        var canonical = n?.Canonical ?? string.Empty;

        if (annotated.Status == RowStatus.Unknown)
        {
            if (_loggedUnknown.Add($"{code}::{canonical}"))
                _logger.LogWarning(
                    "Unknown footprint (needs review): Code='{Code}', Token='{Token}', Raw='{Raw}', Name='{Name}', Value='{Value}', Side='{Side}'",
                    code, canonical, n?.Raw, row.Name, row.Value, row.Side);
            return;
        }

        if (annotated.Status == RowStatus.Rejected)
        {
            if (_loggedRejected.Add($"{code}::{canonical}"))
                _logger.LogInformation(
                    "Placement row rejected: Reason='{Reason}', Name='{Name}', Value='{Value}', FootprintRaw='{Footprint}', Canonical='{Canonical}', Side='{Side}'",
                    code, row.Name, row.Value, row.Footprint, canonical, row.Side);
        }
    }

    private static bool ContainsDnp(string? s) =>
        !string.IsNullOrWhiteSpace(s) &&
        s.Trim().Equals("DNP", StringComparison.OrdinalIgnoreCase);
}