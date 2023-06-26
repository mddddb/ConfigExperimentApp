
using ConfigExperiment.Controllers;
using ConfigExperiment.OptionsTypes;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json.Serialization;
using static System.Collections.Specialized.BitVector32;

namespace ConfigExperiment;

public partial class Program
{
    /// <summary>
    /// Just a delegate so that we don't use unclear Func{T,...TN}
    /// </summary>
    /// <param name="configSection">Configuration section to bind</param>
    /// <param name="typeIdentifierKey">Value that should identify the exact derived type</param>
    /// <param name="serviceProvider">DI container just in case the binding logic has some dependencies</param>
    /// <param name="existingInstance">Existing instance to bind to in case there is one, instead of creating a new instance</param>
    /// <returns></returns>
    private delegate IOptionsBase? BinderDelegate(IConfigurationSection configSection, string typeIdentifierKey, IServiceProvider serviceProvider, IOptionsBase? existingInstance);

    // when we know about all the derived types in the package where we call CustomBind method
    static BinderDelegate binderWithNoDependencies = (section, typeKey, _, existingInstance) =>
    {
        if (existingInstance is not null)
        {
            section.Bind(existingInstance);
            return existingInstance;
        }

        return typeKey switch
        {
            "derived1" => section.Get<DerivedType1>(),
            "derived2" => section.Get<DerivedType2>(),
            _ => null
        };
    };

    // when we don't know about all the derived types in the package where we call CustomBind method, and different handlers for derived types would be added into the service collection
    static BinderDelegate binderWithTypeBindersAsDependency = (section, typeKey, sp, existingInstance) =>
    {
        var typeBinder = sp.GetRequiredService<IEnumerable<IConfigurationBinderForTypeIdentifier>>().First(x => x.TypeIdentifierKey == typeKey);

        typeBinder.Bind(section, ref existingInstance);

        return existingInstance;
    };

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigurationBinderForTypeIdentifier, DerivedType1ConfigurationBinder>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigurationBinderForTypeIdentifier, DerivedType2ConfigurationBinder>());

        ConfigureOptions(builder, binderWithTypeBindersAsDependency);

        builder.Services.AddControllers()
            .AddNewtonsoftJson(o =>
            {
                o.SerializerSettings.ContractResolver = new DefaultContractResolver();
                // o.SerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All;
                o.SerializerSettings.TypeNameAssemblyFormatHandling = Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple;
            });

        var app = builder.Build();

        app.Use((context, next) =>
        {
            var endpoint = context.GetEndpoint();

            return next();
        });


        app.UseHttpsRedirection();

        app.MapControllers();

        app.Run();
    }

    private static void ConfigureOptions(WebApplicationBuilder builder, BinderDelegate binderDelegate)
    {
        IConfigurationSection configurationSection;

        // SingleInstance
        configurationSection = builder.Configuration.GetSection("SingleInstance_DerivedType1");
        builder.Services.AddOptions<SingleInstanceOptions>("1")
            .CustomBind(configurationSection, (_, o, sp) =>
            {
                o.Item = binderDelegate(o.ItemAsSection, o.ItemAsSection["_Type"]!, sp, o.Item);
            }, binderOptions => binderOptions.BindNonPublicProperties = true);

        configurationSection = builder.Configuration.GetSection("SingleInstance_DerivedType2");
        builder.Services.AddOptions<SingleInstanceOptions>("2")
            .CustomBind(configurationSection, (_, o, sp) =>
            {
                o.Item = binderDelegate(o.ItemAsSection, o.ItemAsSection["_Type"]!, sp, o.Item);
            }, binderOptions => binderOptions.BindNonPublicProperties = true);

        // List
        configurationSection = builder.Configuration.GetSection("List");
        builder.Services.AddOptions<ListOptions>()
            .CustomBind(configurationSection, (_, o, sp) =>
            {
                o.Items.AddRange(o.ItemsAsSections
                    .Select(section => binderDelegate(section, section["_Type"]!, sp, existingInstance: null) ?? throw new InvalidOperationException()));
            }, binderOptions => binderOptions.BindNonPublicProperties = true);

        // Dictionary
        configurationSection = builder.Configuration.GetSection("Dictionary");
        builder.Services.AddOptions<DictionaryOptions>()
            .CustomBind(configurationSection, (_, o, sp) =>
            {
                foreach (var kvp in o.ItemsAsDictionaryOfSections)
                {
                    o.Items[Guid.Parse(kvp.Key)] = binderDelegate(kvp.Value, kvp.Value["_Type"]!, sp, existingInstance: null)!;
                }
            }, binderOptions => binderOptions.BindNonPublicProperties = true);

        // Dictionary_KeyIsTypeIdentifier
        configurationSection = builder.Configuration.GetSection("Dictionary_KeyIsTypeIdentifier");
        builder.Services.AddOptions<DictionaryOptions_KeyIsTypeIdentifier>()
            .CustomBind(configurationSection, (_, o, sp) =>
            {
                foreach (var kvp in o.ItemsAsDictionaryOfSections)
                {
                    o.Items[kvp.Key] = binderDelegate(kvp.Value, kvp.Key, sp, existingInstance: null)!;
                }
            }, binderOptions => binderOptions.BindNonPublicProperties = true);
    }
}