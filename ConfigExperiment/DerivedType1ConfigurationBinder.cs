using ConfigExperiment.OptionsTypes;

namespace ConfigExperiment;

internal sealed class DerivedType1ConfigurationBinder : IConfigurationBinderForTypeIdentifier
{
    public string TypeIdentifierKey => "derived1";

    public void Bind(IConfigurationSection configSection, ref IOptionsBase? options)
    {
        if (options is null)
        {
            options = configSection.Get<DerivedType1>();
        }
        else
        {
            configSection.Bind(options);
        }
    }
}