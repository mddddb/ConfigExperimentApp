using System.ComponentModel;

namespace ConfigExperiment;

#pragma warning disable CS8618 // Options type, values will be populated in Bind/Configure
public class AppRegistryEntryOptions
{
    public string UnifiedAppRegistryIdentifier { get; set; }
    public string DisplayName { get; set; }

    [ConfigurationKeyName(nameof(AuthArtifactIssuers))]
    internal List<IConfigurationSection> AuthArtifactIssuersAsSections { get; set; } = new();

    [ConfigurationKeyName("___invalidConfigSectionKeyOnPurpose___")]
    public List<ParsedAuthArtifactIssuerBase> AuthArtifactIssuers { get; set; } = new List<ParsedAuthArtifactIssuerBase>();
}
#pragma warning restore CS8618