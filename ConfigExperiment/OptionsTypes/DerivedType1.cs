namespace ConfigExperiment.OptionsTypes;

public class DerivedType1 : IOptionsBase
{
    public string SharedNameProperty { get; set; }

    public List<string> SharedNameCollectionProperty { get; set; }
}
