using System.ComponentModel;

namespace ConfigExperiment;

/// <summary>
/// Options type with a simple property, where the value is polymorphic.
/// </summary>
public class SingleInstanceOptions
{
    public string SomeRegularProperty { get; set; }

    [ConfigurationKeyName(nameof(Item))]
    internal IConfigurationSection ItemAsSection { get; set; }

    [ConfigurationKeyName("___invalidConfigSectionKeyOnPurpose___")]
    public IOptionsBase? Item { get; set; }
}
