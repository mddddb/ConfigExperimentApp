using ConfigExperiment.OptionsTypes;
using CustomConfigurationBinder;
using Newtonsoft.Json.Serialization;

namespace ConfigExperiment;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        ConfigureOptions(builder);

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

    private static void ConfigureOptions(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ICustomConfigurationBinder, CustomConfigBinder>();

        IConfigurationSection? configurationSection = null;

        // SingleInstance
        configurationSection = builder.Configuration.GetSection("SingleInstance_DerivedType1");
        builder.Services.AddOptions<SingleInstanceOptions>("1")
            .CustomBind(configurationSection);

        configurationSection = builder.Configuration.GetSection("SingleInstance_DerivedType2");
        builder.Services.AddOptions<SingleInstanceOptions>("2")
            .CustomBind(configurationSection);

        // List
        configurationSection = builder.Configuration.GetSection("List");
        builder.Services.AddOptions<ListOptions>()
            .CustomBind(configurationSection);

        // Dictionary
        configurationSection = builder.Configuration.GetSection("Dictionary");
        builder.Services.AddOptions<DictionaryOptions>()
            .CustomBind(configurationSection);

        // Dictionary_KeyIsTypeIdentifier
        configurationSection = builder.Configuration.GetSection("Dictionary_KeyIsTypeIdentifier");
        builder.Services.AddOptions<DictionaryOptions_KeyIsTypeIdentifier>()
            .CustomBind(configurationSection);
    }

    private class CustomConfigBinder : ICustomConfigurationBinder
    {
        public object? Bind(string typeIdentifierKey, IConfiguration config)
        {
            return typeIdentifierKey switch
            {
                "derived1" => config.Get<DerivedType1>(),
                "derived2" => config.Get<DerivedType2>(),
                _ => null
            };
        }
    }
}