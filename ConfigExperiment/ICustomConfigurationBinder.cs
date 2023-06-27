namespace ConfigExperiment;

internal interface ICustomConfigurationBinder
{
    IOptionsBase? Bind(string typeIdentifierKey, IConfigurationSection configSection);
}
