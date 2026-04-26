using EToolkit.Application.Logger;

namespace EToolkit.Application;

/// <summary>
/// Scoped per request, dedup HashSet resets automatically on each new import.
/// Coordinates when logging occurs via IAnnotatedRowLogPolicy,
/// and delegates how to IAnnotatedRowLogger.
/// Has no knowledge of log format, levels, or ILogger infrastructure.
/// </summary>
public sealed class RecordIssueCollector : IRecordIssueCollector
{
    private readonly IAnnotatedRowLogger _logger;
    private readonly IAnnotatedRowLogPolicy _policy;

    private readonly HashSet<string> _reported = new(StringComparer.Ordinal);

    public RecordIssueCollector(IAnnotatedRowLogger logger, IAnnotatedRowLogPolicy policy)
    {
        _logger = logger;
        _policy = policy;
    }

    // Dedup key: "{code}::{canonical}" — prevents log flood when hundreds
    // of rows share the same footprint within a single file.
    public void Report(AnnotatedRow annotated)
    {
        if (!_policy.ShouldLog(annotated))
            return;

        // Policy decides whether this row status warrants any log entry
        var code = annotated.RejectCode ?? "UNKNOWN";
        var canonical = annotated.Normalized?.Canonical ?? string.Empty;

        // Skip if this code+canonical combination has already been reported in this request
        if (!_reported.Add($"{code}::{canonical}"))
            return;

        // Delegate format and output to the logger implementation
        _logger.Log(annotated);
    }
}