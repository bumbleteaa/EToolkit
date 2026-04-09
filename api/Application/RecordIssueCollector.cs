namespace EToolkit.Application;

/// <summary>
/// Implementasi Scoped: satu instance per request, sehingga dedup HashSet reset
/// otomatis setiap import baru — setiap operasi import adalah konteks independen.
/// </summary>
public sealed class RecordIssueCollector : IRecordIssueCollector
{
    private readonly ILogger<RecordIssueCollector> _logger;

    // Dedup key: "{code}::{canonical}" — mencegah log banjir ketika ratusan baris
    // memiliki footprint yang sama dalam satu file.
    private readonly HashSet<string> _loggedUnknown = new(StringComparer.Ordinal);
    private readonly HashSet<string> _loggedRejected = new(StringComparer.Ordinal);

    public RecordIssueCollector(ILogger<RecordIssueCollector> logger)
    {
        _logger = logger;
    }

    // LogAnnotated dari RecordFilteringService dipindahkan ke sini verbatim,
    // dengan penyesuaian nama method saja.
    public void Report(AnnotatedRow annotated)
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
        // RowStatus.Accepted tidak perlu dilaporkan — bukan sinyal yang butuh perhatian.
    }
}