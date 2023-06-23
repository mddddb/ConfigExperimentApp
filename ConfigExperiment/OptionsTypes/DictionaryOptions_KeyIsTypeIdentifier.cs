using System.ComponentModel;

namespace ConfigExperiment;

public class DictionaryOptions_KeyIsTypeIdentifier
{
    public string SomeRegularProperty { get; set; }

    [ConfigurationKeyName(nameof(Items))]
    internal Dictionary<string, IConfigurationSection> ItemsAsDictionaryOfSections { get; set; } = new();

    [ConfigurationKeyName("___invalidConfigSectionKeyOnPurpose___")]
    public Dictionary<string, IOptionsBase> Items { get; set; } = new();

}
