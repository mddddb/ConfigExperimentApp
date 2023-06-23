using System.ComponentModel;

namespace ConfigExperiment;

public class DictionaryOptions
{
    public string SomeRegularProperty { get; set; }

    [ConfigurationKeyName(nameof(Items))]
    internal Dictionary<string, IConfigurationSection> ItemsAsDictionaryOfSections { get; set; } = new();

    [ConfigurationKeyName("___invalidConfigSectionKeyOnPurpose___")]
    public Dictionary<Guid, IOptionsBase> Items { get; set; } = new();

}
