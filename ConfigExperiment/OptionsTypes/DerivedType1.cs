namespace ConfigExperiment.OptionsTypes;

public class DerivedType1 : IPolymorphic
{
    public string SharedNameProperty { get; set; }

    public List<string> SharedNameCollectionProperty { get; set; }
}
