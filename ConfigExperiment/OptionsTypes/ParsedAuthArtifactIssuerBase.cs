using Newtonsoft.Json;

namespace ConfigExperiment;

public abstract class ParsedAuthArtifactIssuerBase
{
    /// <summary>
    /// App identifier containing hierarchical parts that compose a unique identifier.
    /// </summary>
    public abstract IEnumerable<string> AppIdentifierParts { get; }

    [JsonProperty("$issuerType", Order = -2)]
    public string IssuerTypeKey => GetType().Name;
}