using System.ComponentModel;

namespace ConfigExperiment;

public class SingleInstanceOptions
{
    public string SomeRegularProperty { get; set; }


    [ConfigurationKeyName(nameof(Item))]
    internal IConfigurationSection ItemAsSection { get; set; }

    [ConfigurationKeyName("___invalidConfigSectionKeyOnPurpose___")]
    public IOptionsBase? Item { get; set; }
}
