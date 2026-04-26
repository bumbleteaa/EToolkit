using Microsoft.Extensions.Logging;

namespace EToolkit.Application.Logger;

/// <summary>
/// Concrete implementation of IAnnotatedRowLogger.
/// Owns the format and log level for each row status.
/// Moved verbatim from RecordIssueCollector.Report() — behavior is unchanged.
/// </summary>
public sealed class AnnotatedRowLogger : IAnnotatedRowLogger
{
    private readonly ILogger<AnnotatedRowLogger> _logger;

    public AnnotatedRowLogger(ILogger<AnnotatedRowLogger> logger)
    {
        _logger = logger;
    }

    // Emits LogWarning for Unknown rows, LogInformation for Rejected rows.
    // Accepted rows produce no output — they are not a signal that requires attention.
    public void Log(AnnotatedRow annotated)
    {
        var row = annotated.Row;
        var n = annotated.Normalized;
        var code = annotated.RejectCode ?? "UNKNOWN";
        var canonical = n?.Canonical ?? string.Empty;

        if (annotated.Status == RowStatus.Unknown)
        {
            // Unknown footprints need operator review — emitted as warnings
            _logger.LogWarning(
                  "[TP-4] Unknown footprint after normalization: Code='{Code}', Token='{Token}', Raw='{Raw}', Name='{Name}', Value='{Value}', Side='{Side}'",
                code, canonical, n?.Raw, row.Name, row.Value, row.Side);
            return;
        }

        if (annotated.Status == RowStatus.Rejected)
        {
            // Rejected rows are expected outcomes — emitted as informational
            if (n is null)
            {
                _logger.LogInformation(
                    "[TP-3] Pre-filter rejection (normalizer not reached): Code='{Code}', Name='{Name}', Value='{Value}', Footprint='{Footprint}', Side='{Side}'",
                    code, row.Name, row.Value, row.Footprint, row.Side);
                return;
            }

            // TP-4: post-normalizer rejection — Normalized is populated, kind was NonPlaceable.
            _logger.LogInformation(
                "[TP-4] Post-normalizer rejection: Code='{Code}', Name='{Name}', Value='{Value}', FootprintRaw='{Raw}', Canonical='{Canonical}', Side='{Side}'",
                code, row.Name, row.Value, n.Raw, n.Canonical, row.Side);
        }
    }
}