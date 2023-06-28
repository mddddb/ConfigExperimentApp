using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class CustomOptionsBuilderConfigurationExtensions
{
    // original method: https://source.dot.net/#Microsoft.Extensions.Options.ConfigurationExtensions/OptionsBuilderConfigurationExtensions.cs,28

    /// <summary>
    /// Very similar to <see cref="OptionsBuilderConfigurationExtensions.Bind{TOptions}(OptionsBuilder{TOptions}, IConfiguration, Action{BinderOptions}?)"/>,
    /// with the exception of also having <paramref name="additionalCustomBinder"/>, and using <see cref="IConfigurationSection"/> instead of <see cref="IConfiguration"/>.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="optionsBuilder">The options builder to add the services to.</param>
    /// <param name="configSection">The configuration section being bound.</param>
    /// <param name="additionalCustomBinder">
    /// A delegate to run right after the default binding from <paramref name="configSection"/> to <typeparamref name="TOptions"/> finishes.
    /// To be used to inject some custom binding logic.
    /// </param>
    /// <param name="configureBinder">Used to configure the <see cref="BinderOptions"/>.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
    public static OptionsBuilder<TOptions> CustomBind<TOptions>(
        this OptionsBuilder<TOptions> optionsBuilder,
        IConfigurationSection configSection,
        CustomBindingDelegate<TOptions> additionalCustomBinder,
        Action<BinderOptions>? configureBinder = null)
        where TOptions : class
    {
        optionsBuilder.Services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(new ConfigurationChangeTokenSource<TOptions>(optionsBuilder.Name, configSection));

        // similar to the original Bind method, with the exception of additionalCustomBinder being added into the same IConfigureOptions<TOptions>, that does the actual binding from IConfiguration to TOptions
        optionsBuilder.Services.AddOptions<TOptions>(optionsBuilder.Name)
            .Configure<IServiceProvider>((options, sp) =>
            {
                configSection.Bind(options, configureBinder);

                additionalCustomBinder(configSection, options, sp);
            });

        return optionsBuilder;
    }

    /// <param name="configSection">Configuration section being bound to <typeparamref name="TOptions"/>.</param>
    /// <param name="options">
    /// <typeparamref name="TOptions"/> instance that already has the values populated by the regular .NET binding.
    /// This delegate will put more details into it, and in some cases use some properties from it as an input to populate more properties inside.
    /// </param>
    /// <param name="serviceProvider">DI container, in case the custom binding has some dependencies.</param>
    public delegate void CustomBindingDelegate<TOptions>(IConfigurationSection configSection, TOptions options, IServiceProvider serviceProvider);
}