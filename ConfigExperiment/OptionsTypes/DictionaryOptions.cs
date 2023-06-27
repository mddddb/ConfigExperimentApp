namespace ConfigExperiment;

/// <summary>
/// Options type with a dictionary property, where the values are polymorphic.
/// </summary>
/// <remarks>
/// Each IConfigurationSection mapped to IOptionsBase has "_Type" property to identify the exact implementation of IOptionsBase
/// </remarks>
public class DictionaryOptions
{
    public string SomeRegularProperty { get; set; }

    public Dictionary<string, IOptionsBase> Items { get; set; } = new();
}
