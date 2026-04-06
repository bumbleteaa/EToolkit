using EToolkit.Infrastructure;

namespace EToolkit.Application;

public sealed class RecordFilteringService : IRecordFilteringService
{
    private readonly ILogger<RecordFilteringService> _logger;
    private readonly IFootprintNormalizer _normalizer;

    private readonly HashSet<string> _loggedRejected = new(StringComparer.Ordinal);
    private readonly HashSet<string> _loggedGeneric = new(StringComparer.Ordinal);

    public RecordFilteringService(ILogger<RecordFilteringService> logger, IFootprintNormalizer normalizer)
    {
        _logger = logger;
        _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
    }

    public IEnumerable<CsvComponentPlacementRow> FilteredRecord(IEnumerable<CsvComponentPlacementRow> rows)
    {
        foreach (var row in rows)
        {
            if (!IsPlaceable(row))
                continue;

            yield return row;
        }
    }

    private bool IsPlaceable(CsvComponentPlacementRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Footprint))
        {
            LogRejected("EMPTY_VALUE", row, normal: null);
            return false;
        }

        if (ContainsDnp(row.Value) || ContainsDnp(row.Desc) || ContainsDnp(row.Footprint) || ContainsDnp(row.Name))
        {
            LogRejected("DNP", row, normal: null);
            return false;
        }

        var n = _normalizer.NormalizeFootprint(row.Footprint);

        return n.Kind switch
        {
            NormalizedKind.StandardPackage => true,
            NormalizedKind.GenericFootprint => LogGenericAndReturn(row, n),
            NormalizedKind.NonPlaceable => LogRejectedAndReturn("NON_PLACEABLE", row, n),
            _ => LogRejectedAndReturn("UNKNOWN_FOOTPRINT", row, n),
        };
    }

    private static bool ContainsDnp(string? s) =>
        !string.IsNullOrWhiteSpace(s) &&
        s.Trim().Equals("DNP", StringComparison.OrdinalIgnoreCase);

    private bool LogGenericAndReturn(CsvComponentPlacementRow row, NormalizedFootprint normal)
    {
        var dedupeKey = $"GENERIC::{normal.Canonical}";
        if (_loggedGeneric.Add(dedupeKey))
            _logger.LogWarning(
                "Generic footprint: Token='{Token}', Raw='{Raw}', Name='{Name}', Value='{Value}', Desc='{Desc}', Side='{Side}'",
                normal.Canonical, normal.Raw, row.Name, row.Value, row.Desc, row.Side);

        return false;
    }

    private bool LogRejectedAndReturn(string reason, CsvComponentPlacementRow row, NormalizedFootprint? normal)
    {
        LogRejected(reason, row, normal);
        return false;
    }

    private void LogRejected(string reason, CsvComponentPlacementRow row, NormalizedFootprint? normal)
    {
        var canonical = normal?.Canonical ?? string.Empty;
        var dedupeKey = $"{reason}::{canonical}";
        if (!_loggedRejected.Add(dedupeKey))
            return;

        _logger.LogInformation(
            "Placement row rejected: Reason='{Reason}', Name='{Name}', Value='{Value}', FootprintRaw='{Footprint}', Canonical='{Canonical}', Kind='{Kind}', Side='{Side}'",
            reason, row.Name, row.Value, row.Footprint, canonical, normal?.Kind.ToString(), row.Side);
    }
}