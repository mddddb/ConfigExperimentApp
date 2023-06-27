namespace ConfigExperiment;

/// <summary>
/// Options type with a simple property, where the value is polymorphic.
/// </summary>
public class SingleInstanceOptions
{
    public string SomeRegularProperty { get; set; }

    public IOptionsBase? Item { get; set; }
}
