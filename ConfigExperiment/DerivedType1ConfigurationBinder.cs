using ConfigExperiment.OptionsTypes;
using CustomConfigurationBinder;

namespace ConfigExperiment;

internal sealed class DerivedType1ConfigurationBinder : ICustomConfigurationBinder
{
    public string TypeIdentifierKey => "derived1";

    public void Bind(IConfigurationSection configSection, ref object? options)
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