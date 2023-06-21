namespace ConfigExperiment.Controllers;

public static class TestOptionsRegistrationExtensions
{
    public static IServiceCollection RegisterTestOptions(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        services.AddOptions<AppRegistryEntryOptions>()
            .Bind(configurationSection, binderOptions => binderOptions.BindNonPublicProperties = true)
            .Configure(o =>
            {
                o.AuthArtifactIssuers.AddRange(o.AuthArtifactIssuersAsSections
                    .Select(section =>
                    {
                        // one required property in all of the config sections to identify the binder/parser from config section to a needed type
                        const string binderKeyPropertyName = "AuthArtifactIssuerType";

                        var key = section.GetValue<string>(binderKeyPropertyName);
                        if (key == null)
                            throw new InvalidOperationException();

                        // in a real service, parsers/binders would be registered in DI
                        return (ParsedAuthArtifactIssuerBase)(key switch
                        {
                            "AadAppIdWithAadIssuer" => section.Get<AadTokenIssuer>(),
                            "SubstrateAppIdWithSubstrateAppTokenIssuer" => section.Get<SubstrateAppTokenIssuer>(),
                            "PKI" => section.Get<PkiCertificateIssuer>(),
                            _ => throw new ArgumentOutOfRangeException(binderKeyPropertyName)
                        })!;
                    }));
            });

        return services;
    }
}