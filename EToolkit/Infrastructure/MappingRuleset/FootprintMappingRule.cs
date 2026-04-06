namespace Infrastructure.MappingRuleset
{
    // * This class represents a footprint mapping rule, which defines how to map a specific footprint (FromFootprint) to another footprint (ToFootprint) during the normalization process. It also includes an optional note that can provide additional information about the mapping rule. The NormalizeKey and NormalizeValue methods are used to ensure that the input strings are properly trimmed and handled as null values when necessary. 
    public sealed class FootprintMappingRule
    {
        public FootprintMappingRule(string fromFootprint, string toFootprint, string? note = null)
        {
            fromFootprint = NormalizeKey(fromFootprint);
            toFootprint = NormalizeValue(toFootprint);
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }

        public string FromFootprint { get; }
        public string ToFootprint { get; }
        public string? Note { get; }

        internal static string NormalizeKey(string s) => (s ?? string.Empty).Trim();
        internal static string NormalizeValue(string s) => (s ?? string.Empty).Trim();
    }
}