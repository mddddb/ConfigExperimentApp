using ConfigExperiment.OptionsTypes;
using Newtonsoft.Json.Serialization;

namespace ConfigExperiment;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        ConfigureOptions(builder);

        builder.Services
            .AddControllers()
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

    private static void ConfigureOptions(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ICustomConfigurationBinder, CustomConfigBinder>();

        IConfigurationSection configurationSection;

        #region SingleInstance
        configurationSection = builder.Configuration.GetSection("SingleInstance_DerivedType1");
        builder.Services.AddOptions<SingleInstanceOptions>("1")
            .CustomBind(configurationSection, (_, o, sp) =>
            {
                var customConfigBinder = sp.GetRequiredService<ICustomConfigurationBinder>();

                o.Item = customConfigBinder.Bind(o.ItemAsSection["_Type"]!, o.ItemAsSection);
            }, binderOptions => binderOptions.BindNonPublicProperties = true);

        configurationSection = builder.Configuration.GetSection("SingleInstance_DerivedType2");
        builder.Services.AddOptions<SingleInstanceOptions>("2")
            .CustomBind(configurationSection, (_, o, sp) =>
            {
                var customConfigBinder = sp.GetRequiredService<ICustomConfigurationBinder>();

                o.Item = customConfigBinder.Bind(o.ItemAsSection["_Type"]!, o.ItemAsSection);
            }, binderOptions => binderOptions.BindNonPublicProperties = true);

        configurationSection = builder.Configuration.GetSection("SingleInstance_DerivedType2");
        builder.Services.AddOptions<SingleInstanceOptions>("2_withoutConfigSectionProperty")
            .CustomBind(configurationSection, (configSection, o, sp) =>
            {
                var customConfigBinder = sp.GetRequiredService<ICustomConfigurationBinder>();

                var polymorphicConfigSection = configSection.GetRequiredSection(nameof(SingleInstanceOptions.Item));
                var typeIdentifierKey = polymorphicConfigSection.GetValue<string>("_Type")!;

                o.Item = customConfigBinder.Bind(typeIdentifierKey, polymorphicConfigSection);
            });
        #endregion

        #region List
        configurationSection = builder.Configuration.GetSection("List");
        builder.Services.AddOptions<ListOptions>()
            .CustomBind(configurationSection, (_, o, sp) =>
            {
                var customConfigBinder = sp.GetRequiredService<ICustomConfigurationBinder>();

                o.Items.AddRange(o.ItemsAsSections
                    .Select(section => customConfigBinder.Bind(section["_Type"]!, section) ?? throw new InvalidOperationException()));
            }, binderOptions => binderOptions.BindNonPublicProperties = true);

        builder.Services.AddOptions<ListOptions>("withoutConfigSectionProperty")
            .CustomBind(configurationSection, (configSection, o, sp) =>
            {
                var customConfigBinder = sp.GetRequiredService<ICustomConfigurationBinder>();

                var polymorphicConfigSections = configSection
                    .GetRequiredSection(nameof(ListOptions.Items))
                    .GetChildren();

                o.Items.AddRange(polymorphicConfigSections
                    .Select(section => customConfigBinder.Bind(section["_Type"]!, section) ?? throw new InvalidOperationException()));
            });
        #endregion

        #region Dictionary
        configurationSection = builder.Configuration.GetSection("Dictionary");
        builder.Services.AddOptions<DictionaryOptions>()
            .CustomBind(configurationSection, (_, o, sp) =>
            {
                var customConfigBinder = sp.GetRequiredService<ICustomConfigurationBinder>();

                foreach (var kvp in o.ItemsAsDictionaryOfSections)
                {
                    o.Items[Guid.Parse(kvp.Key)] = customConfigBinder.Bind(kvp.Value["_Type"]!, kvp.Value)!;
                }
            }, binderOptions => binderOptions.BindNonPublicProperties = true);
        #endregion

        #region Dictionary_KeyIsTypeIdentifier
        configurationSection = builder.Configuration.GetSection("Dictionary_KeyIsTypeIdentifier");
        builder.Services.AddOptions<DictionaryOptions_KeyIsTypeIdentifier>()
            .CustomBind(configurationSection, (_, o, sp) =>
            {
                var customConfigBinder = sp.GetRequiredService<ICustomConfigurationBinder>();

                foreach (var kvp in o.ItemsAsDictionaryOfSections)
                {
                    o.Items[kvp.Key] = customConfigBinder.Bind(kvp.Key, kvp.Value)!;
                }
            }, binderOptions => binderOptions.BindNonPublicProperties = true);
        #endregion
    }

    private class CustomConfigBinder : ICustomConfigurationBinder
    {
        // method receives a type identifier that is similar to the $type in Json.NET (except being explicitly registered instead of reflection),
        // and tries to bind/parse a config section into the found type
        public IOptionsBase? Bind(string typeIdentifierKey, IConfigurationSection configSection)
        {
            return typeIdentifierKey switch
            {
                "derived1" => configSection.Get<DerivedType1>(),
                "derived2" => configSection.Get<DerivedType2>(),
                _ => null
            };
        }
    }
}