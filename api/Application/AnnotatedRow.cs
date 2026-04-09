using EToolkit.Infrastructure;

namespace EToolkit.Application;

public enum RowStatus
{
    Accepted,
    Unknown,
    Rejected
}

public sealed record AnnotatedRow(
    CsvComponentPlacementRow Row,
    RowStatus Status,
    string? RejectCode,
    NormalizedFootprint? Normalized
);