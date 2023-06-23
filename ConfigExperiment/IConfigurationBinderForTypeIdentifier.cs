namespace ConfigExperiment;

internal interface IConfigurationBinderForTypeIdentifier
{
    string TypeIdentifierKey { get; }
    void Bind(IConfigurationSection configSection, ref IOptionsBase? options);
}
