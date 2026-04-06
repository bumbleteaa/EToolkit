using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace EToolkit.Infrastructure
{
    // ! This class represents a ruleset version, which contains a set of rules for normalizing the footprint of components. It can be used to define different normalization rules for different versions of the ruleset, and to apply the appropriate rules based on the version specified in the input data.
    public sealed class RulesetVersion
    {

        //The constructor takes a version string and a mapping hash string, which are used to identify the ruleset version and the mapping used for normalization. If the version or mapping hash is null or whitespace, they are set to default values ("v0" for version and "UNKNOWN" for mapping hash).
        public RulesetVersion(string version, string mappingHash)
        {
            Version = string.IsNullOrWhiteSpace(version) ? "v0" : version.Trim();
            MappingHash = string.IsNullOrWhiteSpace(mappingHash) ? "UNKNOWN" : mappingHash.Trim();
        }
        public string Version { get; }
        public string MappingHash { get; }

        public override string ToString() => $"{Version} ({MappingHash})";
    }

}