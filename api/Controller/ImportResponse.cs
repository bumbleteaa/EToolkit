namespace EToolkit.Controller;

public sealed record ImportRowDto(
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