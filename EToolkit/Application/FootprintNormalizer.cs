namespace EToolkit.Application;

// TODO : Make new category for filtering => JEDEC footprint
public sealed class FootprintNormalizer
{
    private static readonly HashSet<String> PassiveComponent = new(StringComparer.Ordinal)
    {
        "0201",
        "0402",
        "0603",
        "0805",
        "1206",
        "1210",
        "2010",
        "2512"
    };

    private static readonly HashSet<String> JedecComponent = new(StringComparer.Ordinal)
    {
        "SOT23", 
        "SOT235", 
        "SOT223",
        "SOT89",
        "SOD123",
        "SOD323",
        "SOD523",
        "TO252",
        "TO263"
    };
    
    //Normalize 
    public NormalizedFootprint Normalize(string raw)
    {
        //Empty component
        if (string.IsNullOrWhiteSpace(raw)) 
            return new NormalizedFootprint(raw ?? string.Empty, string.Empty, string.Empty, NormalizedKind.Unknown, reason: "Empty");
        
        var key = ToAlnumKey(raw);
        
        //Passive component
        var passive = TryNormalizePassive(raw);
        if (passive is not null)
            return new NormalizedFootprint(raw, key, passive, NormalizedKind.StandardPackage, reason: "Passive");
        
        //JEDEC packages component
        if (JedecComponent.Contains(key))
            return new NormalizedFootprint(raw, key, key, NormalizedKind.StandardPackage, reason: "JEDEC");
        
        var family = TryDetectFamilyFootprint(key);
        if (family is not null)
            return new NormalizedFootprint(raw, key, key, NormalizedKind.FamilyFootprint, family, reason: "FamilyPrefix");
        var generic = TryDetectGenericFootprint(key);
            return new NormalizedFootprint(raw, key, generic, NormalizedKind.GenericFootprint, reason: "GenericFootprint" );
            
        return new NormalizedFootprint(raw, key, key, NormalizedKind.Unknown, reason: "Unknown");
    }
    
    private static string ToAlnumKey(string raw) => 
        string.IsNullOrEmpty(raw) 
            ? string.Empty
            : new string(raw.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string? TryNormalizePassive(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        
        var digits = new string(raw.ToUpperInvariant().Where(char.IsDigit).ToArray());

        if (digits.Length == 3) digits = "0" + digits;
        if (digits.Length != 4) return null;
        
        return PassiveComponent.Contains(digits) ? digits: null;
    }
    
    //Finding the family footprint package, often found in main IC or STM32 family
    private static string? TryDetectFamilyFootprint(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (!key.Any(Char.IsDigit)) return null;
        
        if (key.Length < 8 ) return null;
        
        if (key.StartsWith("LQFP", StringComparison.Ordinal)) return "LQFP";
        if (key.StartsWith("QFP", StringComparison.Ordinal)) return "QFP";
        if (key.StartsWith("QFN", StringComparison.Ordinal)) return "QFN";
        
        
        return null;
    }

    private static string? TryDetectGenericFootprint(string key) => key is "SM" or "CAP" or "IND" or "DSC" ? key : null;
    
    //StandardPackage = PassiveComponent + JedecComponent
    public enum NormalizedKind {StandardPackage, FamilyFootprint, GenericFootprint, Unknown}
    
    public record NormalizedFootprint(
        string Raw, string Key, string Canonical, NormalizedKind Kind, string? Family = null, string? reason = null);
    
}