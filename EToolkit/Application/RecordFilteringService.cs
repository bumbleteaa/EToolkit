using EToolkit.Infrastructure;

namespace EToolkit.Application;

public sealed class RecordFilteringService : IRecordFilteringService
{
    private readonly ILogger<RecordFilteringService> _logger;
    private readonly FootprintNormalizer _normalizer;

    // Deduplicate logs per canonical footprint
    private readonly HashSet<string> _loggedRejected = new(StringComparer.Ordinal);
    private readonly HashSet<string> _loggedGeneric = new(StringComparer.Ordinal);

    public RecordFilteringService(ILogger<RecordFilteringService> logger, FootprintNormalizer normalizer)
    {
        _logger = logger;
        _normalizer = normalizer;
    }



    //Don't return any value if !IsPlaceable
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

        //Rule 0: Block all empty value
        if (string.IsNullOrWhiteSpace(row.Value))
        {
            LogRejected("EMPTY_VALUE", row, normal: null);
            return false;
        }
        //Rule 1: DNP is non negotiable
        if (ContainsDnp(row.Value) || ContainsDnp(row.Desc) || ContainsDnp(row.Footprint) || ContainsDnp(row.Name))
        {
            LogRejected("DNP", row, normal: null);
            return false;
        }

        //Rule 2: Allow eligible footprint
        var n = _normalizer.NormalizeFootprint(row.Footprint);

        switch (n.Kind)
        {
            case FootprintNormalizer.NormalizedKind.StandardPackage:
                return true;
            case FootprintNormalizer.NormalizedKind.GenericFootprint:
                LogGeneric(row, n);
                return false;
            case FootprintNormalizer.NormalizedKind.NonPlaceable:
                LogRejected("NON_PLACEABLE", row, n);
                return false;
            case FootprintNormalizer.NormalizedKind.Unknown:
            default:
                LogRejected("UNKNOWN_FOOTPRINT", row, n);
                return false;
        }
    }
    private static bool ContainsDnp(string? s) =>
        !string.IsNullOrWhiteSpace(s) &&
        s.Trim().Equals("DNP", StringComparison.OrdinalIgnoreCase);

    private void LogGeneric(CsvComponentPlacementRow row, FootprintNormalizer.NormalizedFootprint normal)
    {
        var dedupeKey = $"GENERIC::{normal.Canonical}";
        if (!_loggedGeneric.Contains(dedupeKey))
            return;

        _logger.LogWarning("This generic footprint: Token='{Token}', Raw='{Raw}', Name='{Name}', Value='{Value}', Desc='{Desc}', Side='{Side}'",
        normal.Canonical, normal.Raw, row.Name, row.Value, row.Desc, row.Side);
    }

    private void LogRejected(string reason, CsvComponentPlacementRow row,
        FootprintNormalizer.NormalizedFootprint? normal)
    {
        var canonical = normal?.Canonical ?? string.Empty;
        var dedupekey = $"{reason}::{canonical}";
        if (!_loggedRejected.Add(dedupekey))
            return;

        _logger.LogInformation("Placement row rejected: Reason='{Reason}', Name='{Name}', Value='{Value}', FootprintRaw='{Footprint}', Canonical='{Canonical}', Kind='{Kind}', Side='{Side}'",
            reason, row.Name, row.Value, row.Footprint, canonical, normal?.Kind.ToString(), row.Side);
    }
}