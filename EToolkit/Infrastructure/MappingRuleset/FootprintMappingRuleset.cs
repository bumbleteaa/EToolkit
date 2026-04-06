using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EToolkit.Application;
using EToolkit.Domain;
using EToolkit.Infrastructure;

namespace Infrastructure.MappingRuleset
{



    // * FootprintMappingRuleset is a class that represents a collection of footprint mapping rules, along with version information for the ruleset. It provides functionality to apply the mapping rules to input footprints and to compute a deterministic hash for the ruleset based on its content. The TryMap method allows for mapping an input footprint to a canonical footprint using the defined rules, while the ComputeDeterministicHash method generates a consistent hash value for the ruleset, enabling version tracking and comparison.*
    public sealed class FootprintMappingRuleset
    {
        private readonly Dictionary<string, FootprintMappingRule> _rulesByFromFootprint;

        public FootprintMappingRuleset(string version, IEnumerable<FootprintMappingRule> rules)
        {
            var list = (rules ?? Enumerable.Empty<FootprintMappingRule>()).Where(r => r != null).ToList();

            var dictionary = new Dictionary<string, FootprintMappingRule>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in list)
            {
                if (string.IsNullOrWhiteSpace(r.FromFootprint))
                    continue; // Skip rules with empty FromFootprint

                dictionary[r.FromFootprint] = r; // This will overwrite duplicates, keeping the last one
            }
            _rulesByFromFootprint = new ReadOnlyDictionary<string, FootprintMappingRule>(dictionary);

            var mappingHash = ComputeMappingHash(_rulesByFromFootprint.Values);
            VersionInfo = new RulesetVersion(version, mappingHash);
        }

        public RulesetVersion VersionInfo { get; }
        public IReadOnlyCollection<FootprintMappingRule> Rules => _rulesByFromFootprint.Values.ToList().AsReadOnly();

        // * TryMap method attempts to map an input footprint to a canonical footprint using the defined mapping rules. It takes an optional input footprint and outputs the corresponding canonical footprint and the applied mapping rule if a match is found. The method returns true if a mapping was successfully applied, and false otherwise. It handles null or whitespace input by treating them as empty strings and ensures that the mapping is case-insensitive.
        public bool TryMap(string? inputFootprint, out string? canonicalFootprint, out FootprintMappingRule? appliedRule)
        {
            canonicalFootprint = null;
            appliedRule = null;

            var key = (inputFootprint ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key))
                return false;
            if (_rulesByFromFootprint.TryGetValue(key, out var rule))
            {
                canonicalFootprint = rule.ToFootprint;
                appliedRule = rule;
                return true;
            }

            return false;
        }

        // * The ComputeMappingHash method computes a deterministic hash for a given set of footprint mapping rules. It first creates a canonical representation of the rules by selecting the relevant properties (FromFootprint, ToFootprint, and Note) and sorting them in a consistent order. Then, it serializes this canonical representation to JSON and computes a SHA256 hash of the resulting byte array. Finally, it converts the hash to a hexadecimal string and returns it as the mapping hash. This allows for consistent identification of the ruleset version based on its content, regardless of the order in which the rules were defined or any extraneous whitespace.*
        public static string ComputeDeterministicHash(IEnumerable<FootprintMappingRule> rules)
        {
            var canonical = rules
                .Where(r => r != null)
                .Select(r => new CanonicalRule(r.FromFootprint, r.ToFootprint, r.Note))
                .OrderBy(r => r.FromFootprint, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.ToFootprint, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var json = JsonSerializer.Serialize(canonical, JsonOptions.Canonical);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(bytes);

            var stringBuilder = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                stringBuilder.Append(b.ToString("x2"));
            return stringBuilder.ToString();
        }

        private sealed class CanonicalRule(string FromFootprint, string ToFootprint);

        // * MappingReport is a class that represents the result of applying a footprint mapping ruleset to a set of input footprints. It contains information about the ruleset version used, the mapping policy applied, any unknown footprints that were encountered during the mapping process, and any changes that were made to the footprints as a result of applying the mapping rules. The status of the mapping process is derived based on the presence of unknown footprints and the mapping policy, indicating whether the mapping was successful, if there are warnings, or if further mapping is needed.
        public sealed class MappingReport
        {
            public MappingReport(
            RulesetVersion ruleset,
            MappingPolicy policy,
            IReadOnlyCollection<UnknownFootprintAgg> unknownFootprints,
            IReadOnlyCollection<MappingChange> changes)
            {
                Ruleset = ruleset ?? new RulesetVersion("v0", "UNKNOWN");
                Policy = policy;
                UnknownFootprints = unknownFootprints ?? Array.Empty<UnknownFootprintAgg>();
                Changes = changes ?? Array.Empty<MappingChange>();
                Status = DeriveStatus(Policy, UnknownFootprints, Changes);
            }

            public RulesetVersion Ruleset { get; }
            public MappingPolicy Policy { get; }
            public MappingStatus Status { get; }
            public IReadOnlyCollection<UnknownFootprintAgg> UnknownFootprints { get; }
            public IReadOnlyCollection<MappingChange> Changes { get; }

            // * Mapping status is derived based on the presence of unknown footprints and the mapping policy. If there are no unknown footprints, the status is Ok. If there are unknown footprints and the policy requires gating on unknown footprints, the status is NeedsMapping. If there are unknown footprints but the policy does not require gating, the status is OkWithWarnings.
            private static MappingStatus DeriveStatus(MappingPolicy policy, IReadOnlyCollection<UnknownFootprintAgg> unknownFootprints, IReadOnlyCollection<MappingChange> changes)
            {
                var hasUnknowns = unknownFootprints != null && unknownFootprints.Count > 0;
                if (!hasUnknowns) return MappingStatus.Ok;

                return policy.ExportGateOnUnknownFootprints
                ? MappingStatus.NeedsMapping
                : MappingStatus.OkWithWarnings;
            }

        }

        // * UnknownFootprintAgg represents an aggregation of unknown footprints encountered during the mapping process. It includes the footprint itself, the count of occurrences, and a collection of examples that illustrate where the unknown footprint was found. This class is used to provide insights into which footprints were not recognized by the mapping rules and to help identify potential gaps in the ruleset that may need to be addressed.*
        public sealed class UnknownFootprintAgg
        {
            public UnknownFootprintAgg(string footprint, int count, IReadOnlyCollection<UnknownFootprintExample> examples)
            {
                Footprint = (footprint ?? string.Empty).Trim();
                Count = count < 0 ? 0 : count;
                Examples = examples ?? Array.Empty<UnknownFootprintExample>();
            }

            public string Footprint { get; }
            public int Count { get; }
            public IReadOnlyCollection<UnknownFootprintExample> Examples { get; }
        }

        // * UnknownFootprintExample represents an example of an unknown footprint encountered during the mapping process. It includes information about the component associated with the unknown footprint, the name and value of the property that contained the unknown footprint, and the unknown footprint itself. This class is used to provide specific examples of where unknown footprints were found, helping to illustrate the context in which they were encountered and aiding in troubleshooting and rule refinement.*
        public sealed class UnknownFootprintExample
        {
            public UnknownFootprintExample(string? comp, string? name, string? value, string? footprint)
            {
                Comp = string.IsNullOrWhiteSpace(comp) ? null : comp.Trim();
                Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
                Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                Footprint = string.IsNullOrWhiteSpace(footprint) ? null : footprint.Trim();
            }

            public string? Comp { get; }
            public string? Name { get; }
            public string? Value { get; }
            public string? Footprint { get; }

        }

        // * MappingChange represents a change made to a footprint as a result of applying the mapping rules. It includes information about the component associated with the change, the original footprint (FromFootprint), the new footprint after mapping (ToFootprint), and an optional note that can provide additional context about the change. This class is used to track and report on the specific changes that were made during the mapping process, allowing for better visibility and understanding of how the input footprints were transformed.
        public sealed class MappingChange
        {
            public MappingChange(string? comp, string? fromFootprint, string? toFootprint, string? ruleNote = null)
            {
                Comp = string.IsNullOrWhiteSpace(comp) ? null : comp.Trim();
                FromFootprint = string.IsNullOrWhiteSpace(fromFootprint) ? null : fromFootprint.Trim();
                ToFootprint = string.IsNullOrWhiteSpace(toFootprint) ? null : toFootprint.Trim();
                RuleNote = string.IsNullOrWhiteSpace(ruleNote) ? null : ruleNote.Trim();
            }

            public string? Comp { get; }
            public string? FromFootprint { get; }
            public string? ToFootprint { get; }
            public string? RuleNote { get; }

        }
        // * MappingPolicy represents the policy for handling unknown footprints during the mapping process. It includes a boolean property ExportGateOnUnknownFootprints, which indicates whether the presence of unknown footprints should result in gating (i.e., preventing further processing) or if it should allow the mapping to proceed with warnings. This class is used to define the behavior of the mapping process when encountering unknown footprints, enabling flexibility in how strict or lenient the mapping should be based on the specific requirements of the use case.*
        public sealed class MappingPolicy
        {
            public MappingPolicy(bool exportGateOnUnknownFootprints)
            {
                ExportGateOnUnknownFootprints = exportGateOnUnknownFootprints;
            }

            public bool ExportGateOnUnknownFootprints { get; }
        }

        public enum MappingStatus
        {
            Ok = 0,
            OkWithWarnings = 1,
            NeedsMapping = 2
        }

        // * PipelineReponse is a contract wraper that encapsulates the result of applying a footprint mapping ruleset to input data. It includes the mapped data (Data) and a MappingReport that provides details about the mapping process, including the ruleset version used, the mapping policy applied, any unknown footprints encountered, and any changes made to the footprints. This class is used to return both the transformed data and the associated mapping report together, allowing for comprehensive feedback on the mapping operation.
        public sealed class PipelineResponse<TData>
        {
            public PipelineResponse(TData data, MappingReport mappingReport)
            {
                Data = data;
                MappingReport = mappingReport;
            }

            public TData Data { get; }
            public MappingReport Report { get; }

        }

        public sealed class FootprintMappingEngine
        {
            private readonly FootprintMappingRuleset _ruleset;
            private readonly MappingPolicy _policy;

            public FootprintMappingEngine(FootprintMappingRuleset ruleset, MappingPolicy policy)
            {
                _ruleset = ruleset ?? throw new ArgumentNullException(nameof(ruleset));
                _policy = policy ?? new MappingPolicy(exportGateOnUnknownFootprints: true);
            }

            public PipelineResponse<IReadOnlyCollection<CsvComponentPlacementRow>> Apply(IReadOnlyCollection<CsvComponentPlacementRow> input)
            {
                var rows = (input ?? Array.Empty<CsvComponentPlacementRow>()).ToList();
                var unknownCounter = new Dictionary<string, List<UnknownFootprintExample>, int>(StringComparer.OrdinalIgnoreCase);
                var unknownFootprintAggs = new Dictionary<string, UnknownFootprintAgg>(StringComparer.OrdinalIgnoreCase);
                var changes = new List<MappingChange>();

                foreach (var r in rows)
                {
                    var footprint = (r.Footprint ?? string.Empty).Trim();

                    if (string.IsNullOrWhiteSpace(footprint))
                        continue;

                    if (_ruleset.TryMap(footprint, out var canonicalFootprint, out var appliedRule) && !string.IsNullOrWhiteSpace(canonicalFootprint))
                    {
                        if (!string.Equals(footprint, canonicalFootprint, StringComparison.OrdinalIgnoreCase))
                        {
                            changes.Add(new MappingChange(
                            comp: r.Comp,
                            fromFootprint: footprint,
                            toFootprint: canonicalFootprint,
                            ruleNote: appliedRule?.Note));
                            r.Footprint = canonicalFootprint; // Update the footprint in the original row
                        }
                    }
                    else
                    {
                        if (!unknownCounter.TryGetValue(footprint, out var list))
                        {
                            list = new List<UnknownFootprintExample>();
                            unknownCounter[footprint] = list;
                        }
                        list.Add(new UnknownFootprintExample(
                            comp: r.Comp,
                            name: "Footprint",
                            value: footprint,
                            footprint: footprint));
                    }
                }
                var unknownFootprintAggsList = unknownCounter
                .Select(kvp => new UnknownFootprintAgg(
                    kvp.Key,
                    count: kvp.Value.Count,
                    examples: kvp.Value.AsReadOnly()))
                .OrderByDescending(agg => agg.Count)
                .ThenBy(agg => agg.Footprint, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();

                var report = new MappingReport(
                   _ruleset.VersionInfo,
                   _policy,
                   unknownFootprintAggsList,
                   changes.AsReadOnly());

                return new PipelineResponse<IReadOnlyCollection<CsvComponentPlacementRow>>(rows.AsReadOnly(), report);
            }
        }

        // * This interface defines the contract for a repository that can load and save footprint mapping rulesets. The Load method retrieves the current ruleset, while the Save method allows for updating the ruleset with new or modified rules. This abstraction allows for flexibility in how the rulesets are stored and managed, enabling different implementations (e.g., in-memory, file-based, database) without affecting the core logic of applying the mapping rules.
        public interface IRulesetRepository
        {
            FootprintMappingRuleset Load();
            void Save(FootprintMappingRuleset ruleset);
        }

        public sealed class JsonFileRulesetRepository : IRulesetRepository
        {
            private readonly string _filePath;

            public JsonFileRulesetRepository(string filePath)
            {
                _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            }

            public FootprintMappingRuleset Load()
            {
                if (!File.Exists(_filePath))
                    return new FootprintMappingRuleset("v0", Array.Empty<FootprintMappingRule>());

                var json = File.ReadAllText(_filePath, Encoding.UTF8);
                var dto = JsonSerializer.Deserialize<RulesetDto>(json, JsonOptions.Default);

                var version = dto?.Version ?? "v0";
                var rules = dto?.Rules?.Select(r => new FootprintMappingRule(r.FromFootprint, r.ToFootprint, r.Note)).ToList() ?? new List<FootprintMappingRule>();

                return new FootprintMappingRuleset(version, rules);
            }

            public void Save(FootprintMappingRuleset ruleset)
            {
                if (ruleset == null) throw new ArgumentNullException(nameof(ruleset));

                var dto = new RulesetDto
                {
                    Version = ruleset.VersionInfo.Version,
                    Rules = ruleset.Rules
                    .Select(r => new RuleDto
                    {
                        FromFootprint = r.FromFootprint,
                        ToFootprint = r.ToFootprint,
                        Note = r.Note
                    })
                    .OrderBy(r => r.FromFootprint, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(r => r.ToFootprint, StringComparer.OrdinalIgnoreCase)
                    .ToList()
                };
                var json = JsonSerializer.Serialize(dto, JsonOptions.Pretty);
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? ".");
                File.WriteAllText(_filePath, json, Encoding.UTF8);
            }
            private sealed class RulesetDto
            {
                [JsonPropertyName("version")]
                public string? Version { get; set; }

                [JsonPropertyName("rules")]
                public List<RuleDto>? Rules { get; set; }
            }

            private sealed class RuleDto
            {
                [JsonPropertyName("fromFootprint")]
                public string? FromFootprint { get; set; }

                [JsonPropertyName("toFootprint")]
                public string? ToFootprint { get; set; }

                [JsonPropertyName("note")]
                public string? Note { get; set; }
            }
        }

        internal static class JsonOptions
        {
            public static readonly JsonSerializerOptions Default = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            public static readonly JsonSerializerOptions Pretty = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }
    }
}