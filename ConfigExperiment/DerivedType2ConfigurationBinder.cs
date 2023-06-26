using ConfigExperiment.OptionsTypes;
using CustomConfigurationBinder;

namespace ConfigExperiment;

internal sealed class DerivedType2ConfigurationBinder : ICustomConfigurationBinder
{
    public string TypeIdentifierKey => "derived2";

    public void Bind(IConfigurationSection configSection, ref object? options)
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
