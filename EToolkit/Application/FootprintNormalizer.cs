namespace EToolkit.Application;

// * This class is responsible for normalizing the footprint of a component to a canonical form, which can be used for filtering and grouping components. It also provides a reason for the normalization, which can be used for logging and debugging purposes.

public sealed class FootprintNormalizer
{
    //Passive component are often represented by their size, we can normalize them to a canonical form if they are in the common size list
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

    //JEDEC standard packages, often used for transistors, diodes, voltage regulator, etc. We can normalize them to their standard name
    private static readonly HashSet<String> PlaceableFamiliesComponent = new(StringComparer.Ordinal)
    {
        "SOIC",
        "SO",
        "SOP",
        "TSSOP",
        "SSOP",
        "QFN",
        "QFP",
        "LQFP",
        "TQFP",
        "DFN",
        "BGA",
        "SOT",
        "SOD",
        "TO",
        "DO",
        "SM",
        "TSMT",
        "SMS"
    };

    private static readonly HashSet<String> GenericFootprintKeyword = new(StringComparer.Ordinal)
    {
        "RES", "CAP", "IND", "LED",
    };

    //Non-placeable footprint, often used for mechanical parts, connectors, test points, etc. We can mark them as non-placeable if they contain these keywords
    private static readonly HashSet<String> NonPlaceableFootprint = new(StringComparer.Ordinal)
    {
        "CONN",
        "CON",
        "HDR",
        "HEADER",
        "SHDR",
        "JTAG",
        "TESTPOINT",
        "TP",
        "SILK",
        "MECH",
        "MOUNT",
        "HOLE",
        "FID",
        "FIDUCIAL",
        "DNP",
        "P100",
        "H"
    };

    /*Normalize is the main method of the class, it takes a raw footprint string as input, and returns a NormalizedFootprint record as output. The normalization process follows these rules:
    Rule 0: Block all empty value, and mark them as unknown
    Rule 1: DNP is non negotiable, mark them as unknown
    Rule 2: Allow eligible passive footprint, mark them as standard package
    Rule 3: Allow family footprint, mark them as family footprint if detected
    Rule 4: Try to detect generic footprint, mark them as generic footprint if detected*/
    public NormalizedFootprint NormalizeFootprint(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new NormalizedFootprint(raw ?? string.Empty, string.Empty, string.Empty, NormalizedKind.Unknown, Reason: "Empty");

        var key = ToAlnumKey(raw);

        //* Normalize passive component footprint 
        var passive = TryNormalizePassive(raw);
        if (passive is not null)
            return new NormalizedFootprint(raw, key, passive, NormalizedKind.StandardPackage, Family: "PASSIVE", Reason: "Passive Component");

        var family = ExtractFamilyFootrpint(key);

        //* Normalize any family footprint 
        if (family is null) return new NormalizedFootprint(raw, key, key, NormalizedKind.Unknown, Reason: "Unknown Footprint");

        if (family is not null && NonPlaceableFootprint.Contains(family))
            return new NormalizedFootprint(raw, key, family, NormalizedKind.NonPlaceable, family, Reason: "Non Placeable");

        if (family is not null && PlaceableFamiliesComponent.Contains(family))
            return new NormalizedFootprint(raw, key, key, NormalizedKind.StandardPackage, family, Reason: "Placeable Family Footprint");

        if (family is not null && GenericFootprintKeyword.Contains(family))
            return new NormalizedFootprint(raw, key, key, NormalizedKind.GenericFootprint, family, Reason: "Generic Footprint");

        return new NormalizedFootprint(raw, key, key, NormalizedKind.Unknown, Reason: "Unknown Footprint");

    }

    //Alphanumeric key is used for normalization, it removes all non-alphanumeric characters and convert to upper case. This is because most of the footprint are represented in alphanumeric form, and we want to ignore the case and special characters when comparing footprints.
    private static string ToAlnumKey(string raw) =>
        string.IsNullOrEmpty(raw)
            ? string.Empty
            : new string(raw.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray()); //If the raw string is null or empty, return an empty string as the key, otherwise remove all non-alphanumeric characters and convert to upper case to get the key.

    //TryNormalizePassive is used to normalize the passive component footprint, it checks if the alphanumeric key is in the common passive component list, and return the canonical form if found. This is because most of the passive component are represented by their size, and we want to normalize them to a canonical form for filtering and grouping purposes.
    private static string? TryNormalizePassive(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var digits = new string(raw.ToUpperInvariant().Where(char.IsDigit).ToArray());

        if (digits.Length == 3) digits = "0" + digits;
        if (digits.Length != 4) return null;

        return PassiveComponent.Contains(digits) ? digits : null; // If the digits are in the passive component list, return the digits as the canonical form, otherwise return null
    }

    //TryDetectFamilyFootprint is used to detect if the footprint can be considered as a family footprint, it checks if the alphanumeric key starts with a known family prefix, and return the family name if found. This is because some footprints are represented in a way that the family name is followed by some digits or other characters, and we want to group them together as a family for filtering and grouping purposes.
    private static string? ExtractFamilyFootrpint(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        var i = 0;
        while (i < key.Length && char.IsLetter(key[i])) i++;
        if (i == 0) return null;

        return key[..i]; //If the key starts with letters followed by digits, we can consider the letters as the family prefix, and return it as the canonical form for family footprint. For example, SOT23-5 can be normalized to SOT23 as the family footprint.
    }

    //StandardPackage = PassiveComponent + JedecComponent, FamilyFootprint = Footprint that can be detected as family footprint, GenericFootprint = Footprint that can be detected as generic footprint, Unknown = Footprint that cannot be normalized to a known form.
    public enum NormalizedKind
    {
        StandardPackage,
        NonPlaceable,
        GenericFootprint,
        Unknown
    }

    //record to represent the normalized footprint, it contains the raw footprint, the alphanumeric key, the canonical form, the kind of normalization, the family if it's a family footprint, and the reason for normalization.
    public record NormalizedFootprint(
        string Raw, //The original raw footprint string
        string Key, //The alphanumeric key used for normalization, it removes all non-alphanumeric characters and convert to upper case
        string Canonical, //The canonical form of the footprint, it is used for filtering and grouping components, it can be the same as the key if the footprint cannot be normalized to a known form
        NormalizedKind Kind, //The kind of normalization, it can be StandardPackage, FamilyFootprint, GenericFootprint, or Unknown
        string? Family = null, //The family of the footprint if it's a family footprint, it is used for grouping components that belong to the same family, it can be null if the footprint is not a family footprint
        string? Reason = null); //The reason for normalization, it is used for logging and debugging purposes, it can be null if the normalization is successful and does not need a reason.

}