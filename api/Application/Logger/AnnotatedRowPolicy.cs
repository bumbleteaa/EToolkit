namespace EToolkit.Application.Logger;

/// <summary>
/// Default logging policy based on row classification status.
/// Stateless and side-effect free — safe to register as Singleton.
/// Dedup is intentionally delegated to RecordIssueCollector, not handled here.
/// </summary>
public sealed class AnnotatedRowLogPolicy : IAnnotatedRowLogPolicy
{
    // Only Unknown and Rejected rows warrant a log entry.
    // Accepted rows are the expected happy path and produce no log output.
    public bool ShouldLog(AnnotatedRow annotated) =>
        annotated.Status is RowStatus.Unknown or RowStatus.Rejected;
}