using ConfigExperiment.OptionsTypes;

namespace ConfigExperiment;

internal sealed class DerivedType2ConfigurationBinder : IConfigurationBinderForTypeIdentifier
{
    public string TypeIdentifierKey => "derived2";

    public void Bind(IConfigurationSection configSection, ref IOptionsBase? options)
    {
        if (options is null)
        {
            options = configSection.Get<DerivedType2>();
        }
        else
        {
            configSection.Bind(options);
        }
    }
}
