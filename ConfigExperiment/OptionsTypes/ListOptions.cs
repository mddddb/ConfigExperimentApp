using System.ComponentModel;

namespace ConfigExperiment;

public class ListOptions
{
    public string SomeRegularProperty { get; set; }

    [ConfigurationKeyName(nameof(Items))]
    internal List<IConfigurationSection> ItemsAsSections { get; set; } = new();

    [ConfigurationKeyName("___invalidConfigSectionKeyOnPurpose___")]
    public List<IOptionsBase> Items { get; set; } = new List<IOptionsBase>();
}
