namespace ConfigExperiment;

internal interface ICustomConfigurationBinder
{
    // create a new instance if possible and bind the config section to it
    IPolymorphic? Bind(string typeIdentifierKey, IConfigurationSection configSection);
}
