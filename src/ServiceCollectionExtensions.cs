namespace NKZSoft.Service.Configuration.Swagger;

using Configuration;

public static class ServiceCollectionExtensions
{
    private const string DeprecationDescription = "This API version is obsolete.";

    public static IServiceCollection AddSwagger(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly executingAssembly)
    {
        /*
        we need this here to prevent an error
        if swagger is off (using by controllers to resolve a version)
        */
        services.AddApiVersioning(o =>
        {
            o.ReportApiVersions = true;
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.DefaultApiVersion = new ApiVersion(1, 0);
        });

        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        ArgumentNullException.ThrowIfNull(executingAssembly, nameof(executingAssembly));

        var swaggerConfiguration = configuration.GetSection(SwaggerConfigurationSection.SectionName)
            .Get<SwaggerConfigurationSection>();

        if (swaggerConfiguration.Enabled == false)
        {
            return services;
        }

        var serviceName = executingAssembly.GetName().Name!;

        services.AddVersionedApiExplorer();
        var provider = services.BuildServiceProvider().GetService<IApiVersionDescriptionProvider>();

        services.AddSwaggerGen(c =>
        {
            //Well about !
            //I'd rather NRE here than not sure why an empty swagger
            foreach (var description in provider!.ApiVersionDescriptions)
            {
                c.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description, serviceName));
            }

            c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            c.CustomSchemaIds(x => x.FullName);
            c.EnableAnnotations();

            c.TagActionsBy(api => new[] { api.GroupName });
            c.DocInclusionPredicate((name, api) => true);

            var xmlFile = $"{serviceName}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlFile))
            {
                c.IncludeXmlComments(xmlPath);
            }

            if (swaggerConfiguration.AuthorizationEnabled != false)
            {
                return;
            }

            var securitySchema = new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };

            c.AddSecurityDefinition("Bearer", securitySchema);

            var securityRequirement = new OpenApiSecurityRequirement { { securitySchema, new[] { "Bearer" } } };

            c.AddSecurityRequirement(securityRequirement);
        });

        return services;
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description, string serviceName)
    {
        var info = new OpenApiInfo
        {
            Title = $"swagger {serviceName}",
            Version = description.ApiVersion.ToString()
        };

        if (description.IsDeprecated)
        {
            info.Description += DeprecationDescription;
        }

        return info;
    }
}
