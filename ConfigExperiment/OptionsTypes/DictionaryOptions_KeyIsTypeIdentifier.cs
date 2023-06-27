namespace ConfigExperiment;

/// <summary>
/// Options type with a dictionary property, where the values are polymorphic.
/// </summary>
/// <remarks>
/// The exact implementation of IOptionsBase is identified by the key of each KeyValuePair.
/// </remarks>
public class DictionaryOptions_KeyIsTypeIdentifier
{
    public string SomeRegularProperty { get; set; }

    public Dictionary<string, IOptionsBase> Items { get; set; } = new();
}
