namespace EToolkit.Application;

public sealed record PipelineResponse<TData>(
    TData Data,
    PipelineReport Report);

public sealed record PipelineReport(
    string Stage,
    bool IsExportReady,
    string? RulesetVersion,
    IReadOnlyList<PipelineIssue> Issues);

public sealed record PipelineIssue(
    string Code,
    Severity Severity,
    string Message,
    IssueContext Context);

public enum Severity
{
    Info,
    Warning,
    Error
}

public sealed record IssueContext(
    string? FootprintRaw = null,
    string? FootprintKey = null,
    string? FootprintCanonical = null,
    string? Name = null,
    string? Side = null,
    int? RowNumber = null,
    int? Count = null);