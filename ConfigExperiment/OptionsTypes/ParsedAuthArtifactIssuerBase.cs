using Newtonsoft.Json;

namespace ConfigExperiment;

#pragma warning disable CS8618 // Options type, values will be populated in Bind/Configure
public abstract class ParsedAuthArtifactIssuerBase
{
    /// <summary>
    /// App identifier containing hierarchical parts that compose a unique identifier.
    /// </summary>
    public abstract IEnumerable<string> AppIdentifierParts { get; }

    public string AuthArtifactIssuerType { get; set; }
}
#pragma warning restore CS8618