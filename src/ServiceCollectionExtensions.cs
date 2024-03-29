﻿namespace NKZSoft.Service.Configuration.Swagger;

using Configuration;

public static class ServiceCollectionExtensions
{
    private const string DeprecationDescription = "This API version is obsolete.";

    /// <summary>
    /// Adds Swagger service to the specified services collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the MassTransits to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> containing settings to be used.</param>
    /// <param name="executingAssembly">The assembly that contains the code that is currently executing.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
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

        ArgumentNullException.ThrowIfNull(swaggerConfiguration, nameof(swaggerConfiguration));

        if (swaggerConfiguration.Enabled == false)
        {
            return services;
        }

        var serviceName = executingAssembly.GetName().Name!;

        services.AddVersionedApiExplorer();
        var provider = services.BuildServiceProvider().GetService<IApiVersionDescriptionProvider>();

        ArgumentNullException.ThrowIfNull(provider);

        services.AddSwaggerGen(c =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                c.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description, serviceName));
            }

            c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            c.CustomSchemaIds(x => x.FullName);
            c.EnableAnnotations();

            c.DocInclusionPredicate((name, api) =>
            {
                var actionApiVersionModel = api.ActionDescriptor
                    .GetApiVersionModel(ApiVersionMapping.Implicit | ApiVersionMapping.Explicit);

                if (actionApiVersionModel == null)
                {
                    return true;
                }

                return actionApiVersionModel.DeclaredApiVersions.Any() ?
                    actionApiVersionModel.DeclaredApiVersions.Any(dv => dv.ToString() == name) :
                    actionApiVersionModel.ImplementedApiVersions.Any(dv => dv.ToString() == name);
            });

            var xmlFile = $"{serviceName}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
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
