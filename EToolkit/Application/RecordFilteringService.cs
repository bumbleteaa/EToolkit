using EToolkit.Infrastructure;

namespace EToolkit.Application;

public class RecordFilteringService : IRecordFilteringService
{
    private static readonly HashSet<string> AllowedFootprints = new(StringComparer.OrdinalIgnoreCase)
    {
        "R0603",
        "603",
        "805"
    };

    public IEnumerable<CsvComponentPlacementRow> FilterByFootprint(IEnumerable<CsvComponentPlacementRow> rows)
    {
        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.Footprint)) continue;
            
            var footPrint = row.Footprint.Trim().ToUpperInvariant();
            
            if (!AllowedFootprints.Contains(footPrint)) continue;
            
            yield return row;
        }
    }
}