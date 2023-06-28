using System.ComponentModel;

namespace ConfigExperiment;

/// <summary>
/// Options type with a List property, where the values are polymorphic.
/// </summary>
public class ListOptions
{
    public string SomeRegularProperty { get; set; }

    [ConfigurationKeyName(nameof(Items))]
    internal List<IConfigurationSection> ItemsAsSections { get; set; } = new();

    [ConfigurationKeyName("___invalidConfigSectionKeyOnPurpose___")]
    public List<IPolymorphic> Items { get; set; } = new List<IPolymorphic>();
}
