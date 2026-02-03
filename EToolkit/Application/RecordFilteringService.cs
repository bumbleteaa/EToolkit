using EToolkit.Infrastructure;

namespace EToolkit.Application;

public class RecordFilteringService : IRecordFilteringService
{
    private static readonly HashSet<string> AllowedFootprints = new(StringComparer.OrdinalIgnoreCase)
    {
        "R0603",
        "603",
        "805",
        "1206",
        "SOIC",         // If footprint is too much, why we not block the DNP instead?
        "SOIC 12",
        "so8",
        "soic8",
        "TSSOP14",
        "SOP50P310X90-8N",
        "SM7745HEV660M",
        "C1210",
    };
    
    //Don't return any value if !IsPlaceable
    public IEnumerable<CsvComponentPlacementRow> FilteredRecord(IEnumerable<CsvComponentPlacementRow> rows)
    {
        foreach (var row in rows)
        {
            if (!IsPlacable(row))
                continue;

            yield return row;
        }
    }

    private bool IsPlacable(CsvComponentPlacementRow row)
    {
        //Initial rule: Block all empty value
        if (string.IsNullOrWhiteSpace(row.Value))
            return false; //If empty, skip

        //Rule 1: DNP is non negotiable
        if (row.Value.Trim().Equals("DNP", StringComparison.OrdinalIgnoreCase)) 
            return false;
        
        //Rule 2: Allow eligible footprint
        var footprint = row.Footprint.Trim();
        
        if (!AllowedFootprints.Contains(footprint))
            return false;

        //Question :
        // If EDA footprint is very random depends on engineers, what a rule that make the filtering works?
        // What if we make the "and" logic? more than one condition is valid? But what make the source of truth?

        //Proposed Rule :
        // Check if ANY of these fields contain DNP indicators: Look in the Value field, then the Description field,
        // then the Footprint field, then the Component Name field, and check each one for patterns that indicate
        // "do not populate." If I find DNP markers in any of these locations, block that row.

        //Rule Layer
        // 1) Block any DNP Value (Non Negotiable)
        // 2) Allow some footprint that eligible, but
        // 3)  whitelist when the Value is containing "C, R, H, U" 
        
        return true;
    }
}