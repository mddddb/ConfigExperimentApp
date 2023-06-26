using CustomConfigurationBinder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class CustomOptionsBuilderConfigurationExtensions
{
    // original method: https://source.dot.net/#Microsoft.Extensions.Options.ConfigurationExtensions/OptionsBuilderConfigurationExtensions.cs,28

    /// <summary>
    /// Very similar to <see cref="OptionsBuilderConfigurationExtensions.Bind{TOptions}(OptionsBuilder{TOptions}, IConfiguration, Action{BinderOptions}?)"/>,
    /// with the exception of also having <paramref name="additionalCustomBinder"/>.
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
        Action<IConfiguration, TOptions, IServiceProvider> additionalCustomBinder,
        Action<BinderOptions>? configureBinder = null)
        where TOptions : class
    {
        optionsBuilder.Services.AddOptions();
        optionsBuilder.Services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(new ConfigurationChangeTokenSource<TOptions>(optionsBuilder.Name, configSection));

        // similar to the original Bind method, with the exception of additionalCustomBinder being added into the same IConfigureOptions<TOptions>, that does the actual binding from IConfiguration to TOptions
        optionsBuilder.Services.AddOptions<TOptions>(optionsBuilder.Name)
            .Configure<IServiceProvider>((options, sp) =>
            {
                new NamedConfigureFromConfigurationOptions<TOptions>(optionsBuilder.Name, configSection, configureBinder)
                    .Configure(optionsBuilder.Name, options);

                additionalCustomBinder(configSection, options, sp);
            });

        return optionsBuilder;
    }

    public static OptionsBuilder<TOptions> CustomBind<TOptions>(
        this OptionsBuilder<TOptions> optionsBuilder,
        IConfigurationSection configSection,
        Action<BinderOptions>? configureBinder = null)
        where TOptions : class
    {
        optionsBuilder.Services.TryAddSingleton<CustomConfigurationBindersProvider>();

        var rootConfigSectionPathLength = configSection.Path.Split(':').Length;

        optionsBuilder.CustomBind(
            configSection,
            (config, options, sp) => PerformRecursiveBinding(configSection, options, sp.GetRequiredService<CustomConfigurationBindersProvider>()),
            configureBinder);

        return optionsBuilder;

        void PerformRecursiveBinding(IConfigurationSection childConfigSection, TOptions optionsInstance, CustomConfigurationBindersProvider customBinderProvider)
        {
            var typeIdentifier = childConfigSection["_Type"];
            if (typeIdentifier is not null && childConfigSection.Path != configSection.Path)
            {
                var customBinder = customBinderProvider.Get(typeIdentifier);

                // open question: what to do if no binder is registered? Since the decision might be to throw or rather just skip binding that one, depending on what is wanted.
                if(customBinder is not null)
                {
                    var polymorphicPropertyPathFromTOptions = childConfigSection.Path.Split(':').SkipLast(1).Skip(rootConfigSectionPathLength); // path parts for the polymorphic property, from TOptions perspective (first string is a TOptions property name)
                    var tuple = GetPolymorphicPropInfoAndItsParentValue(optionsInstance, polymorphicPropertyPathFromTOptions);
                    if(tuple is not null)
                    {
                        var polymorphicPropType = tuple.Value.propertyInfo.PropertyType;

                        // only the List scenario for the time being
                        if (polymorphicPropType.IsGenericType && (polymorphicPropType.GetGenericTypeDefinition() == typeof(List<>)))
                        {
                            var listValue = tuple.Value.propertyInfo.GetValue(tuple.Value.parentValue) as System.Collections.IList;

                            // if there is a collection instance - just add items in there. Otherwise - set a new collection instance.
                            if (listValue is null)
                            {
                                listValue = Activator.CreateInstance(polymorphicPropType) as System.Collections.IList ?? throw new InvalidOperationException();
                                tuple.Value.propertyInfo.SetValue(tuple.Value.parentValue, listValue);
                            }

                            object? boundListItem = null;

                            customBinder.Bind(childConfigSection, ref boundListItem);

                            listValue.Add(boundListItem);
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                var childrenSections = childConfigSection.GetChildren();
                foreach (var childSection in childrenSections)
                {
                    PerformRecursiveBinding(childSection, optionsInstance, customBinderProvider);
                }
            }
        }

        (PropertyInfo propertyInfo, object parentValue)? GetPolymorphicPropInfoAndItsParentValue(object source, IEnumerable<string> path)
        {
            object parentValue = source;
            object? childValue = source;
            PropertyInfo? propertyInfo = null;
            foreach(var pathPart in path)
            {
                if(childValue is null)
                {
                    return null;
                }

                parentValue = childValue;
                propertyInfo = source.GetType().GetProperty(pathPart);
                childValue = propertyInfo!.GetValue(parentValue);
            }

            return (propertyInfo!, parentValue);
        }
    }
}

internal sealed class CustomConfigurationBindersProvider
{
    private readonly Dictionary<string, ICustomConfigurationBinder> _customBindersDict;

    public CustomConfigurationBindersProvider(IEnumerable<ICustomConfigurationBinder> customBinders)
    {
        _customBindersDict = customBinders
            .ToDictionary(x => x.TypeIdentifierKey);
    }

    public ICustomConfigurationBinder? Get(string typeIdentifier)
    {
        _ = _customBindersDict.TryGetValue(typeIdentifier, out var customBinder);

        return customBinder;
    }
}