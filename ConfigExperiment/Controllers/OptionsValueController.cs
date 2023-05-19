using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace ConfigExperiment.Controllers
{
    [ApiController]
    [Route("/options")]
    public class OptionsValueController : ControllerBase
    {
        private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionGroupCollectionProvider;
        private readonly IServiceProvider _serviceProvider;

        public OptionsValueController(
            IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider,
            IServiceProvider serviceProvider)
        {
            _apiDescriptionGroupCollectionProvider = apiDescriptionGroupCollectionProvider;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        public object Get(
            [FromServices] IOptions<AppRegistryEntryOptions> options,
            [FromServices] IOptionsMonitor<AppRegistryEntryOptions> optionsMonitor,
            [FromServices] IOptionsSnapshot<AppRegistryEntryOptions> optionsSnapshot
            )
        {
            return new
            {
                options = options.Value,
                //optionsMonitor = optionsMonitor.CurrentValue,
                //optionsSnapshot = optionsSnapshot.Value
            };
        }

        [HttpGet("test")]
        [Authorize(AuthenticationSchemes = "Test")]
        public object Get()
        {
            return Ok();
        }

        [HttpGet("docs")]
        public object GetDocs()
        {
            return _apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items
                .SelectMany(apiDescriptionGroup => apiDescriptionGroup.Items)
                .Select(apiDescription =>
                {
                    _ = apiDescription.TryGetMethodInfo(out var methodInfo);
                    var controllerType = (apiDescription.ActionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo;

                    var rawAttributesInfo = CustomAttributeData.GetCustomAttributes(methodInfo)
                        .Select(x => x.ToString());

                    if (controllerType != null)
                    {
                        rawAttributesInfo = CustomAttributeData.GetCustomAttributes(controllerType)
                            .Select(x => $"controller: {x}")
                            .Concat(rawAttributesInfo);
                    }

                    return new
                    {
                        apiDescription.RelativePath,
                        apiDescription.HttpMethod,
                        RawAttributes = rawAttributesInfo,
                        ResponseType = GetTypeSchema(apiDescription.SupportedResponseTypes.FirstOrDefault()?.Type),
                    };
                });
        }

        private static object? GetTypeSchema(Type? type)
        {
            if (type == null)
            {
                return null;
            }

            return new
            {
                Name = type.FullName,
                Properties = type.GetProperties()
                    .Select(x => new
                    {
                        x.Name,
                        Type = x.PropertyType.FullName,
                        Attributes = CustomAttributeData.GetCustomAttributes(x)
                            .Select(attr => attr.ToString())
                            .ToArray()
                    })
            };
        }
    }

#pragma warning disable CS8618 // Options type, values will be populated in Bind/Configure
    public class AppRegistryEntryOptions
    {
        public string UnifiedAppRegistryIdentifier { get; set; }
        public string DisplayName { get; set; }

        [ConfigurationKeyName(nameof(AuthArtifactIssuers))]
        internal List<IConfigurationSection> AuthArtifactIssuersAsSections { get; set; }

        [ConfigurationKeyName("___invalidConfigSectionKeyOnPurpose___")]
        public List<ParsedAuthArtifactIssuerBase> AuthArtifactIssuers { get; set; } = new List<ParsedAuthArtifactIssuerBase>();
    }

    public abstract class ParsedAuthArtifactIssuerBase
    {
        /// <summary>
        /// App identifier containing hierarchical parts that compose a unique identifier.
        /// </summary>
        public abstract IEnumerable<string> AppIdentifierParts { get; }

        [JsonProperty("$issuerType", Order = -2)]
        public string IssuerTypeKey => GetType().Name;
    }

    public class AadTokenIssuer : ParsedAuthArtifactIssuerBase
    {
        public override IEnumerable<string> AppIdentifierParts
        {
            get
            {
                yield return "AAD";
                yield return AadInstance;
                yield return ApplicationId.ToString();
            }
        }
        public string AadInstance { get; set; }
        public Guid ApplicationId { get; set; }

        public List<string> Audiences { get; set; }
        public List<string> AcceptedTokenVersions { get; set; }
    }

    public class SubstrateAppTokenIssuer : ParsedAuthArtifactIssuerBase
    {
        public override IEnumerable<string> AppIdentifierParts
        {
            get
            {
                yield return "Substrate";
                yield return ApplicationId;
            }
        }
        public string ApplicationId { get; set; }
        public TokenValidationOptions SubstrateAppTokenValidationOptions { get; set; }

        public class TokenValidationOptions
        {
            public string Option1 { get; set; }
            public Dictionary<string, string> AdditionalBag { get; set; }
        }
    }

    public class PkiCertificateIssuer : ParsedAuthArtifactIssuerBase
    {
        public override IEnumerable<string> AppIdentifierParts
        {
            get
            {
                yield return "PKI";
                yield return RootIssuer;
                yield return ClientCertificate.SubjectName; // or ClientCertificate.PublicKeyThumbprint
            }
        }
        public string RootIssuer { get; set; }
        public CertificateInfo ClientCertificate { get; set; }

        public class CertificateInfo
        {
            public string SubjectName { get; set; }
            public string PublicKeyThumbprint { get; set; }
        }
    }

#pragma warning restore CS8618

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
                            const string issuerKeyName = "AuthArtifactIssuerType";

                            var key = section.GetValue<string>(issuerKeyName);
                            if (key == null)
                                throw new InvalidOperationException();

                            return (ParsedAuthArtifactIssuerBase)(key switch
                            {
                                "AadAppIdWithAadIssuer" => section.Get<AadTokenIssuer>(),
                                "SubstrateAppIdWithSubstrateAppTokenIssuer" => section.Get<SubstrateAppTokenIssuer>(),
                                "PKI" => section.Get<PkiCertificateIssuer>(),
                                _ => throw new ArgumentOutOfRangeException(issuerKeyName)
                            })!;
                        }));
                });

            return services;
        }
    }
}