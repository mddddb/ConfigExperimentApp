namespace ConfigExperiment
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ApiExplorer;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.AspNetCore.Routing.Template;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.Swagger;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public class SwaggerGenerator
    {
        private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionsProvider;

        private readonly ISchemaGenerator _schemaGenerator;

        private readonly SwaggerGeneratorOptions _options;

        private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

        private static readonly Dictionary<string, OperationType> OperationTypeMap = new Dictionary<string, OperationType>
    {
        {
            "GET",
            (OperationType)0
        },
        {
            "PUT",
            (OperationType)1
        },
        {
            "POST",
            (OperationType)2
        },
        {
            "DELETE",
            (OperationType)3
        },
        {
            "OPTIONS",
            (OperationType)4
        },
        {
            "HEAD",
            (OperationType)5
        },
        {
            "PATCH",
            (OperationType)6
        },
        {
            "TRACE",
            (OperationType)7
        }
    };

        private static readonly Dictionary<BindingSource, ParameterLocation> ParameterLocationMap = new Dictionary<BindingSource, ParameterLocation>
    {
        {
            BindingSource.Query,
            (ParameterLocation)0
        },
        {
            BindingSource.Header,
            (ParameterLocation)1
        },
        {
            BindingSource.Path,
            (ParameterLocation)2
        }
    };

        private static readonly IReadOnlyCollection<KeyValuePair<string, string>> ResponseDescriptionMap = (IReadOnlyCollection<KeyValuePair<string, string>>)(object)new KeyValuePair<string, string>[19]
        {
        new KeyValuePair<string, string>("1\\d{2}", "Information"),
        new KeyValuePair<string, string>("201", "Created"),
        new KeyValuePair<string, string>("202", "Accepted"),
        new KeyValuePair<string, string>("204", "No Content"),
        new KeyValuePair<string, string>("2\\d{2}", "Success"),
        new KeyValuePair<string, string>("304", "Not Modified"),
        new KeyValuePair<string, string>("3\\d{2}", "Redirect"),
        new KeyValuePair<string, string>("400", "Bad Request"),
        new KeyValuePair<string, string>("401", "Unauthorized"),
        new KeyValuePair<string, string>("403", "Forbidden"),
        new KeyValuePair<string, string>("404", "Not Found"),
        new KeyValuePair<string, string>("405", "Method Not Allowed"),
        new KeyValuePair<string, string>("406", "Not Acceptable"),
        new KeyValuePair<string, string>("408", "Request Timeout"),
        new KeyValuePair<string, string>("409", "Conflict"),
        new KeyValuePair<string, string>("429", "Too Many Requests"),
        new KeyValuePair<string, string>("4\\d{2}", "Client Error"),
        new KeyValuePair<string, string>("5\\d{2}", "Server Error"),
        new KeyValuePair<string, string>("default", "Error")
        };

        public SwaggerGenerator(SwaggerGeneratorOptions options, IApiDescriptionGroupCollectionProvider apiDescriptionsProvider, ISchemaGenerator schemaGenerator)
        {
            _options = options ?? new SwaggerGeneratorOptions();
            _apiDescriptionsProvider = apiDescriptionsProvider;
            _schemaGenerator = schemaGenerator;
        }

        public SwaggerGenerator(SwaggerGeneratorOptions options, IApiDescriptionGroupCollectionProvider apiDescriptionsProvider, ISchemaGenerator schemaGenerator, IAuthenticationSchemeProvider authenticationSchemeProvider)
            : this(options, apiDescriptionsProvider, schemaGenerator)
        {
            _authenticationSchemeProvider = authenticationSchemeProvider;
        }

        public async Task<OpenApiDocument> GetSwaggerAsync()
        {
            (IEnumerable<ApiDescription>, OpenApiDocument, SchemaRepository) swaggerDocumentWithoutFilters = GetSwaggerDocumentWithoutFilters();
            IEnumerable<ApiDescription> applicableApiDescriptions = swaggerDocumentWithoutFilters.Item1;
            OpenApiDocument swaggerDoc = swaggerDocumentWithoutFilters.Item2;
            SchemaRepository schemaRepository = swaggerDocumentWithoutFilters.Item3;
            OpenApiComponents components = swaggerDoc.Components;
            components.SecuritySchemes = await GetSecuritySchemes();
            DocumentFilterContext filterContext = new DocumentFilterContext(applicableApiDescriptions, _schemaGenerator, schemaRepository);
            foreach (IDocumentFilter documentFilter in _options.DocumentFilters)
            {
                documentFilter.Apply(swaggerDoc, filterContext);
            }
            swaggerDoc.Components.Schemas = new SortedDictionary<string, OpenApiSchema>(swaggerDoc.Components.Schemas, _options.SchemaComparer);
            return swaggerDoc;
        }

        public OpenApiDocument GetSwagger()
        {
            var (applicableApiDescriptions, swaggerDoc, schemaRepository) = GetSwaggerDocumentWithoutFilters();
            swaggerDoc.Components.SecuritySchemes = GetSecuritySchemes().Result;
            DocumentFilterContext filterContext = new DocumentFilterContext(applicableApiDescriptions, _schemaGenerator, schemaRepository);
            foreach (IDocumentFilter documentFilter in _options.DocumentFilters)
            {
                documentFilter.Apply(swaggerDoc, filterContext);
            }
            swaggerDoc.Components.Schemas = new SortedDictionary<string, OpenApiSchema>(swaggerDoc.Components.Schemas, _options.SchemaComparer);
            return swaggerDoc;
        }

        private (IEnumerable<ApiDescription>, OpenApiDocument, SchemaRepository) GetSwaggerDocumentWithoutFilters()
        {
            IEnumerable<ApiDescription> applicableApiDescriptions = _apiDescriptionsProvider.ApiDescriptionGroups.Items
                .SelectMany((ApiDescriptionGroup @group) => @group.Items);

            SchemaRepository schemaRepository = new SchemaRepository();
            OpenApiDocument swaggerDoc = new OpenApiDocument
            {
                //Info = info,
                //Servers = GenerateServers(host, basePath),
                Paths = GeneratePaths(applicableApiDescriptions, schemaRepository),
                Components = new OpenApiComponents
                {
                    Schemas = schemaRepository.Schemas
                },
                SecurityRequirements = new List<OpenApiSecurityRequirement>(_options.SecurityRequirements)
            };
            return (applicableApiDescriptions, swaggerDoc, schemaRepository);
        }

        private async Task<IDictionary<string, OpenApiSecurityScheme>> GetSecuritySchemes()
        {
            if (!_options.InferSecuritySchemes)
            {
                return new Dictionary<string, OpenApiSecurityScheme>(_options.SecuritySchemes);
            }
            IEnumerable<AuthenticationScheme> enumerable = ((_authenticationSchemeProvider == null) ? Enumerable.Empty<AuthenticationScheme>() : (await _authenticationSchemeProvider.GetAllSchemesAsync()));
            IEnumerable<AuthenticationScheme> authenticationSchemes = enumerable;
            if (_options.SecuritySchemesSelector != null)
            {
                return _options.SecuritySchemesSelector(authenticationSchemes);
            }
            return authenticationSchemes.Where((AuthenticationScheme authScheme) => authScheme.Name == "Bearer").ToDictionary((Func<AuthenticationScheme, string>)((AuthenticationScheme authScheme) => authScheme.Name), (Func<AuthenticationScheme, OpenApiSecurityScheme>)((AuthenticationScheme authScheme) => new OpenApiSecurityScheme
            {
                Type = (SecuritySchemeType)1,
                Scheme = "bearer",
                In = (ParameterLocation)1,
                BearerFormat = "Json Web Token"
            }));
        }

        private IList<OpenApiServer> GenerateServers(string host, string basePath)
        {
            //IL_002f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0034: Unknown result type (might be due to invalid IL or missing references)
            //IL_0046: Expected O, but got Unknown
            if (_options.Servers.Any())
            {
                return new List<OpenApiServer>(_options.Servers);
            }
            if (host != null || basePath != null)
            {
                return new List<OpenApiServer>
            {
                new OpenApiServer
                {
                    Url = host + basePath
                }
            };
            }
            return new List<OpenApiServer>();
        }

        private OpenApiPaths GeneratePaths(IEnumerable<ApiDescription> apiDescriptions, SchemaRepository schemaRepository)
        {
            //IL_0035: Unknown result type (might be due to invalid IL or missing references)
            //IL_003b: Expected O, but got Unknown
            //IL_005b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0060: Unknown result type (might be due to invalid IL or missing references)
            //IL_0073: Expected O, but got Unknown
            IEnumerable<IGrouping<string, ApiDescription>> enumerable = from apiDesc in apiDescriptions.OrderBy(_options.SortKeySelector)
                                                                        group apiDesc by apiDesc.RelativePathSansParameterConstraints();
            OpenApiPaths paths = new OpenApiPaths();
            foreach (IGrouping<string, ApiDescription> group in enumerable)
            {
                ((Dictionary<string, OpenApiPathItem>)(object)paths).Add("/" + group.Key, new OpenApiPathItem
                {
                    Operations = GenerateOperations(group, schemaRepository)
                });
            }
            return paths;
        }

        private IDictionary<OperationType, OpenApiOperation> GenerateOperations(IEnumerable<ApiDescription> apiDescriptions, SchemaRepository schemaRepository)
        {
            //IL_0108: Unknown result type (might be due to invalid IL or missing references)
            IEnumerable<IGrouping<string?, ApiDescription>> enumerable = from apiDesc in apiDescriptions.OrderBy(_options.SortKeySelector)
                                                                         group apiDesc by apiDesc.HttpMethod;
            Dictionary<OperationType, OpenApiOperation> operations = new Dictionary<OperationType, OpenApiOperation>();
            foreach (IGrouping<string, ApiDescription> group in enumerable)
            {
                string httpMethod = group.Key;
                if (httpMethod == null)
                {
                    throw new SwaggerGeneratorException($"Ambiguous HTTP method for action - {group.First().ActionDescriptor.DisplayName}. Actions require an explicit HttpMethod binding for Swagger/OpenAPI 3.0");
                }
                if (group.Count() > 1 && _options.ConflictingActionsResolver == null)
                {
                    throw new SwaggerGeneratorException(string.Format("Conflicting method/path combination \"{0} {1}\" for actions - {2}. Actions require a unique method/path combination for Swagger/OpenAPI 3.0. Use ConflictingActionsResolver as a workaround", httpMethod, group.First().RelativePath, string.Join(",", group.Select((ApiDescription apiDesc) => apiDesc.ActionDescriptor.DisplayName))));
                }
                ApiDescription apiDescription = ((group.Count() > 1) ? _options.ConflictingActionsResolver(group) : group.Single());
                operations.Add(OperationTypeMap[httpMethod.ToUpper()], GenerateOperation(apiDescription, schemaRepository));
            }
            return operations;
        }

        private OpenApiOperation GenerateOperation(ApiDescription apiDescription, SchemaRepository schemaRepository)
        {
            //IL_000c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0011: Unknown result type (might be due to invalid IL or missing references)
            //IL_001e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0035: Unknown result type (might be due to invalid IL or missing references)
            //IL_0043: Unknown result type (might be due to invalid IL or missing references)
            //IL_0051: Unknown result type (might be due to invalid IL or missing references)
            //IL_005f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0076: Expected O, but got Unknown
            OpenApiOperation operation = GenerateOpenApiOperationFromMetadata(apiDescription, schemaRepository);
            try
            {
                if (operation == null)
                {
                    operation = new OpenApiOperation
                    {
                        Tags = GenerateOperationTags(apiDescription),
                        OperationId = _options.OperationIdSelector(apiDescription),
                        Parameters = GenerateParameters(apiDescription, schemaRepository),
                        RequestBody = GenerateRequestBody(apiDescription, schemaRepository),
                        Responses = GenerateResponses(apiDescription, schemaRepository),
                        Deprecated = apiDescription.CustomAttributes().OfType<ObsoleteAttribute>().Any()
                    };
                }
                apiDescription.TryGetMethodInfo(out var methodInfo);
                OperationFilterContext filterContext = new OperationFilterContext(apiDescription, _schemaGenerator, schemaRepository, methodInfo);
                foreach (IOperationFilter operationFilter in _options.OperationFilters)
                {
                    operationFilter.Apply(operation, filterContext);
                }
                return operation;
            }
            catch (Exception ex)
            {
                throw new SwaggerGeneratorException("Failed to generate Operation for action - " + apiDescription.ActionDescriptor.DisplayName + ". See inner exception", ex);
            }
        }

        private OpenApiOperation GenerateOpenApiOperationFromMetadata(ApiDescription apiDescription, SchemaRepository schemaRepository)
        {
            OpenApiOperation operation = (apiDescription.ActionDescriptor?.EndpointMetadata)?.OfType<OpenApiOperation>().SingleOrDefault();
            if (operation == null)
            {
                return null;
            }
            foreach (OpenApiParameter parameter in operation.Parameters)
            {
                ApiParameterDescription apiParameter = apiDescription.ParameterDescriptions.SingleOrDefault((ApiParameterDescription desc) => desc.Name == parameter.Name && !desc.IsFromBody() && !desc.IsFromForm());
                if (apiParameter != null)
                {
                    parameter.Schema = GenerateSchema(apiParameter.ModelMetadata.ModelType, schemaRepository, apiParameter.PropertyInfo(), apiParameter.ParameterInfo(), apiParameter.RouteInfo);
                }
            }
            OpenApiRequestBody requestBody = operation.RequestBody;
            ICollection<OpenApiMediaType> requestContentTypes = ((requestBody == null) ? null : requestBody.Content?.Values);
            if (requestContentTypes != null)
            {
                foreach (OpenApiMediaType content in requestContentTypes)
                {
                    ApiParameterDescription requestParameter = apiDescription.ParameterDescriptions.SingleOrDefault((ApiParameterDescription desc) => desc.IsFromBody() || desc.IsFromForm());
                    if (requestParameter != null)
                    {
                        content.Schema = GenerateSchema(requestParameter.ModelMetadata.ModelType, schemaRepository, requestParameter.PropertyInfo(), requestParameter.ParameterInfo());
                    }
                }
            }
            foreach (KeyValuePair<string, OpenApiResponse> kvp in (Dictionary<string, OpenApiResponse>)(object)operation.Responses)
            {
                OpenApiResponse response = kvp.Value;
                ApiResponseType responseModel = apiDescription.SupportedResponseTypes.SingleOrDefault((ApiResponseType desc) => desc.StatusCode.ToString() == kvp.Key);
                if (responseModel == null)
                {
                    continue;
                }
                ICollection<OpenApiMediaType> responseContentTypes = ((response == null) ? null : response.Content?.Values);
                if (responseContentTypes == null)
                {
                    continue;
                }
                foreach (OpenApiMediaType item in responseContentTypes)
                {
                    item.Schema = GenerateSchema(responseModel.Type, schemaRepository);
                }
            }
            return operation;
        }

        private IList<OpenApiTag> GenerateOperationTags(ApiDescription apiDescription)
        {
            return ((IEnumerable<string>)_options.TagsSelector(apiDescription)).Select((Func<string, OpenApiTag>)((string tagName) => new OpenApiTag
            {
                Name = tagName
            })).ToList();
        }

        private IList<OpenApiParameter> GenerateParameters(ApiDescription apiDescription, SchemaRepository schemaRespository)
        {
            return (from apiParam in apiDescription.ParameterDescriptions
                    where !apiParam.IsFromBody() && !apiParam.IsFromForm() && !apiParam.CustomAttributes().OfType<BindNeverAttribute>().Any() && (apiParam.ModelMetadata == null || apiParam.ModelMetadata.IsBindingAllowed)
                    select GenerateParameter(apiParam, schemaRespository)).ToList();
        }

        private OpenApiParameter GenerateParameter(ApiParameterDescription apiParameter, SchemaRepository schemaRepository)
        {
            //IL_004f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0054: Unknown result type (might be due to invalid IL or missing references)
            //IL_0064: Unknown result type (might be due to invalid IL or missing references)
            //IL_0069: Unknown result type (might be due to invalid IL or missing references)
            //IL_009b: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a7: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a8: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b3: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ba: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c3: Expected O, but got Unknown
            string name = (_options.DescribeAllParametersInCamelCase ? apiParameter.Name.ToCamelCase() : apiParameter.Name);
            ParameterLocation location = (ParameterLocation)((apiParameter.Source != null && ParameterLocationMap.ContainsKey(apiParameter.Source)) ? ((int)ParameterLocationMap[apiParameter.Source]) : 0);
            bool isRequired = apiParameter.IsRequiredParameter();
            object obj;
            if (apiParameter.ModelMetadata == null)
            {
                OpenApiSchema val = new OpenApiSchema();
                obj = (object)val;
                val.Type = "string";
            }
            else
            {
                obj = GenerateSchema(apiParameter.ModelMetadata.ModelType, schemaRepository, apiParameter.PropertyInfo(), apiParameter.ParameterInfo(), apiParameter.RouteInfo);
            }
            OpenApiSchema schema = (OpenApiSchema)obj;
            OpenApiParameter parameter = new OpenApiParameter
            {
                Name = name,
                In = location,
                Required = isRequired,
                Schema = schema
            };
            ParameterFilterContext filterContext = new ParameterFilterContext(apiParameter, _schemaGenerator, schemaRepository, apiParameter.PropertyInfo(), apiParameter.ParameterInfo());
            foreach (IParameterFilter parameterFilter in _options.ParameterFilters)
            {
                parameterFilter.Apply(parameter, filterContext);
            }
            return parameter;
        }

        private OpenApiSchema GenerateSchema(Type type, SchemaRepository schemaRepository, PropertyInfo propertyInfo = null, ParameterInfo parameterInfo = null, ApiParameterRouteInfo routeInfo = null)
        {
            try
            {
                return _schemaGenerator.GenerateSchema(type, schemaRepository, propertyInfo, parameterInfo, routeInfo);
            }
            catch (Exception ex)
            {
                throw new SwaggerGeneratorException($"Failed to generate schema for type - {type}. See inner exception", ex);
            }
        }

        private OpenApiRequestBody GenerateRequestBody(ApiDescription apiDescription, SchemaRepository schemaRepository)
        {
            OpenApiRequestBody requestBody = null;
            RequestBodyFilterContext filterContext = null;
            ApiParameterDescription bodyParameter = apiDescription.ParameterDescriptions.FirstOrDefault((ApiParameterDescription paramDesc) => paramDesc.IsFromBody());
            IEnumerable<ApiParameterDescription> formParameters = apiDescription.ParameterDescriptions.Where((ApiParameterDescription paramDesc) => paramDesc.IsFromForm());
            if (bodyParameter != null)
            {
                requestBody = GenerateRequestBodyFromBodyParameter(apiDescription, schemaRepository, bodyParameter);
                filterContext = new RequestBodyFilterContext(bodyParameter, null, _schemaGenerator, schemaRepository);
            }
            else if (formParameters.Any())
            {
                requestBody = GenerateRequestBodyFromFormParameters(apiDescription, schemaRepository, formParameters);
                filterContext = new RequestBodyFilterContext(null, formParameters, _schemaGenerator, schemaRepository);
            }
            if (requestBody != null)
            {
                foreach (IRequestBodyFilter requestBodyFilter in _options.RequestBodyFilters)
                {
                    requestBodyFilter.Apply(requestBody, filterContext);
                }
                return requestBody;
            }
            return requestBody;
        }

        private OpenApiRequestBody GenerateRequestBodyFromBodyParameter(ApiDescription apiDescription, SchemaRepository schemaRepository, ApiParameterDescription bodyParameter)
        {
            //IL_003a: Unknown result type (might be due to invalid IL or missing references)
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0076: Unknown result type (might be due to invalid IL or missing references)
            //IL_007e: Expected O, but got Unknown
            IEnumerable<string> contentTypes = InferRequestContentTypes(apiDescription);
            bool isRequired = bodyParameter.IsRequiredParameter();
            OpenApiSchema schema = GenerateSchema(bodyParameter.ModelMetadata.ModelType, schemaRepository, bodyParameter.PropertyInfo(), bodyParameter.ParameterInfo());
            return new OpenApiRequestBody
            {
                Content = contentTypes.ToDictionary((Func<string, string>)((string contentType) => contentType), (Func<string, OpenApiMediaType>)((string contentType) => new OpenApiMediaType
                {
                    Schema = schema
                })),
                Required = isRequired
            };
        }

        private IEnumerable<string> InferRequestContentTypes(ApiDescription apiDescription)
        {
            IEnumerable<string> explicitContentTypes = apiDescription.CustomAttributes().OfType<ConsumesAttribute>().SelectMany((ConsumesAttribute attr) => attr.ContentTypes)
                .Distinct();
            if (explicitContentTypes.Any())
            {
                return explicitContentTypes;
            }
            IEnumerable<string> apiExplorerContentTypes = (from format in apiDescription.SupportedRequestFormats
                                                           select format.MediaType into x
                                                           where x != null
                                                           select x).Distinct();
            if (apiExplorerContentTypes.Any())
            {
                return apiExplorerContentTypes;
            }
            return Enumerable.Empty<string>();
        }

        private OpenApiRequestBody GenerateRequestBodyFromFormParameters(ApiDescription apiDescription, SchemaRepository schemaRepository, IEnumerable<ApiParameterDescription> formParameters)
        {
            //IL_0038: Unknown result type (might be due to invalid IL or missing references)
            //IL_003d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0075: Expected O, but got Unknown
            IEnumerable<string> contentTypes = InferRequestContentTypes(apiDescription);
            IEnumerable<string> enumerable2;
            if (!contentTypes.Any())
            {
                IEnumerable<string> enumerable = new string[1] { "multipart/form-data" };
                enumerable2 = enumerable;
            }
            else
            {
                enumerable2 = contentTypes;
            }
            contentTypes = enumerable2;
            OpenApiSchema schema = GenerateSchemaFromFormParameters(formParameters, schemaRepository);
            return new OpenApiRequestBody
            {
                Content = contentTypes.ToDictionary((Func<string, string>)((string contentType) => contentType), (Func<string, OpenApiMediaType>)((string contentType) => new OpenApiMediaType
                {
                    Schema = schema,
                    Encoding = ((IEnumerable<KeyValuePair<string, OpenApiSchema>>)schema.Properties).ToDictionary((Func<KeyValuePair<string, OpenApiSchema>, string>)((KeyValuePair<string, OpenApiSchema> entry) => entry.Key), (Func<KeyValuePair<string, OpenApiSchema>, OpenApiEncoding>)((KeyValuePair<string, OpenApiSchema> entry) => new OpenApiEncoding
                    {
                        Style = (ParameterStyle)2
                    }))
                }))
            };
        }

        private OpenApiSchema GenerateSchemaFromFormParameters(IEnumerable<ApiParameterDescription> formParameters, SchemaRepository schemaRepository)
        {
            //IL_0046: Unknown result type (might be due to invalid IL or missing references)
            //IL_004b: Unknown result type (might be due to invalid IL or missing references)
            //IL_00aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_00af: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ba: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ce: Expected O, but got Unknown
            Dictionary<string, OpenApiSchema> properties = new Dictionary<string, OpenApiSchema>();
            List<string> requiredPropertyNames = new List<string>();
            foreach (ApiParameterDescription formParameter in formParameters)
            {
                string name = (_options.DescribeAllParametersInCamelCase ? formParameter.Name.ToCamelCase() : formParameter.Name);
                object obj;
                if (formParameter.ModelMetadata == null)
                {
                    OpenApiSchema val = new OpenApiSchema();
                    obj = (object)val;
                    val.Type = "string";
                }
                else
                {
                    obj = GenerateSchema(formParameter.ModelMetadata.ModelType, schemaRepository, formParameter.PropertyInfo(), formParameter.ParameterInfo());
                }
                OpenApiSchema schema = (OpenApiSchema)obj;
                properties.Add(name, schema);
                if (formParameter.IsRequiredParameter())
                {
                    requiredPropertyNames.Add(name);
                }
            }
            return new OpenApiSchema
            {
                Type = "object",
                Properties = properties,
                Required = new SortedSet<string>(requiredPropertyNames)
            };
        }

        private OpenApiResponses GenerateResponses(ApiDescription apiDescription, SchemaRepository schemaRepository)
        {
            //IL_001b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0021: Expected O, but got Unknown
            IEnumerable<ApiResponseType> enumerable = apiDescription.SupportedResponseTypes.DefaultIfEmpty(new ApiResponseType
            {
                StatusCode = 200
            });
            OpenApiResponses responses = new OpenApiResponses();
            foreach (ApiResponseType responseType in enumerable)
            {
                string statusCode = (responseType.IsDefaultResponse() ? "default" : responseType.StatusCode.ToString());
                ((Dictionary<string, OpenApiResponse>)(object)responses).Add(statusCode, GenerateResponse(apiDescription, schemaRepository, statusCode, responseType));
            }
            return responses;
        }

        private OpenApiResponse GenerateResponse(ApiDescription apiDescription, SchemaRepository schemaRepository, string statusCode, ApiResponseType apiResponseType)
        {
            //IL_0050: Unknown result type (might be due to invalid IL or missing references)
            //IL_0055: Unknown result type (might be due to invalid IL or missing references)
            //IL_005c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0094: Expected O, but got Unknown
            string description = ResponseDescriptionMap.FirstOrDefault((KeyValuePair<string, string> entry) => Regex.IsMatch(statusCode, entry.Key)).Value;
            IEnumerable<string> responseContentTypes = InferResponseContentTypes(apiDescription, apiResponseType);
            return new OpenApiResponse
            {
                Description = description,
                Content = responseContentTypes.ToDictionary((string contentType) => contentType, (string contentType) => CreateResponseMediaType(apiResponseType.ModelMetadata, schemaRepository))
            };
        }

        private IEnumerable<string> InferResponseContentTypes(ApiDescription apiDescription, ApiResponseType apiResponseType)
        {
            if (apiResponseType.ModelMetadata == null)
            {
                return Enumerable.Empty<string>();
            }
            IEnumerable<string> explicitContentTypes = apiDescription.CustomAttributes().OfType<ProducesAttribute>().SelectMany((ProducesAttribute attr) => attr.ContentTypes)
                .Distinct();
            if (explicitContentTypes.Any())
            {
                return explicitContentTypes;
            }
            IEnumerable<string> apiExplorerContentTypes = apiResponseType.ApiResponseFormats.Select((ApiResponseFormat responseFormat) => responseFormat.MediaType).Distinct();
            if (apiExplorerContentTypes.Any())
            {
                return apiExplorerContentTypes;
            }
            return Enumerable.Empty<string>();
        }

        private OpenApiMediaType CreateResponseMediaType(ModelMetadata modelMetadata, SchemaRepository schemaRespository)
        {
            //IL_0000: Unknown result type (might be due to invalid IL or missing references)
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            //IL_001c: Expected O, but got Unknown
            return new OpenApiMediaType
            {
                Schema = GenerateSchema(modelMetadata.ModelType, schemaRespository)
            };
        }

    }
    public static class Extensions
    {
        internal static string RelativePathSansParameterConstraints(this ApiDescription apiDescription)
        {
            IEnumerable<string> sanitizedSegments = TemplateParser.Parse(apiDescription.RelativePath).Segments.Select((TemplateSegment s) => string.Concat(s.Parts.Select((TemplatePart p) => (p.Name == null) ? p.Text : ("{" + p.Name + "}"))));
            return string.Join("/", sanitizedSegments);
        }
        internal static string ToCamelCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            IEnumerable<string> cameCasedParts = from part in value.Split('.')
                                                 select char.ToLowerInvariant(part[0]) + part.Substring(1);
            return string.Join(".", cameCasedParts);
        }
        internal static bool IsFromBody(this ApiParameterDescription apiParameter)
        {
            return apiParameter.Source == BindingSource.Body;
        }
        internal static bool IsFromForm(this ApiParameterDescription apiParameter)
        {
            BindingSource source = apiParameter.Source;
            Type elementType = apiParameter.ModelMetadata?.ElementType;
            if (!(source == BindingSource.Form) && !(source == BindingSource.FormFile))
            {
                if (elementType != null)
                {
                    return typeof(IFormFile).IsAssignableFrom(elementType);
                }
                return false;
            }
            return true;
        }

        internal static bool IsDefaultResponse(this ApiResponseType apiResponseType)
        {
            PropertyInfo propertyInfo = apiResponseType.GetType().GetProperty("IsDefaultResponse");
            if (propertyInfo != null)
            {
                return (bool)propertyInfo.GetValue(apiResponseType);
            }
            return false;
        }
    }
}