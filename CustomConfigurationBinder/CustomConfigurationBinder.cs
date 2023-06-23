using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class CustomOptionsBuilderConfigurationExtensions
{
    public static OptionsBuilder<TOptions> CustomBind<TOptions>(
        this OptionsBuilder<TOptions> optionsBuilder,
        IConfiguration config,
        Action<IConfiguration, TOptions, IServiceProvider> additionalCustomBinder,
        Action<BinderOptions>? configureBinder = null)
        where TOptions : class
    {
        optionsBuilder.Services.AddOptions();
        optionsBuilder.Services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(new ConfigurationChangeTokenSource<TOptions>(optionsBuilder.Name, config));

        // similar to the original Bind method, with the exception of additionalCustomBinder being added into the same IConfigureOptions<TOptions>, that does the actual binding from IConfiguration to TOptions
        optionsBuilder.Services.AddOptions<TOptions>()
            .Configure<IServiceProvider>((options, sp) =>
            {
                new NamedConfigureFromConfigurationOptions<TOptions>(optionsBuilder.Name, config, configureBinder)
                    .Configure(options);

                additionalCustomBinder(config, options, sp);
            });

        return optionsBuilder;
    }

}