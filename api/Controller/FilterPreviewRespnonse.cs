namespace EToolkit.Controllers;

public sealed record FilterPreviewRowDto(
    int RowIndex,
    string Status,
    string Comp,
    string Name,
    string Value,
    string Footprint,
    string Desc,
    string Side,
    string[] Issues
);

public sealed record FilterPreviewDataDto(
    int TotalCount,
    IReadOnlyList<FilterPreviewRowDto> Rows,
    bool IsTruncated,
    int LimitApplied
);