namespace ConfigExperiment;

/// <summary>
/// Options type with a List property, where the values are polymorphic.
/// </summary>
public class ListOptions
{
    public string SomeRegularProperty { get; set; }

    public List<IOptionsBase> Items { get; set; } = new List<IOptionsBase>();
}
