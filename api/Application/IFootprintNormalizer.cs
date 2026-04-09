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

public interface IFootprintNormalizer
{
    NormalizedFootprint NormalizeFootprint(string raw);
}