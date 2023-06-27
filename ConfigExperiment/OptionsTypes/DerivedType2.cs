namespace ConfigExperiment.OptionsTypes;

public class DerivedType2 : IOptionsBase
{
    public bool SharedNameProperty { get; set; }

    public Dictionary<string, int> SharedNameCollectionProperty { get; set; }
}
