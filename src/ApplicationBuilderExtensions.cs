namespace NKZSoft.Service.Configuration.Swagger;

using Configuration;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Register the Swagger middleware
    /// </summary>
    /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> containing settings to be used.</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseSwagger(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var swaggerConfiguration = configuration.GetSection(SwaggerConfigurationSection.SectionName)
            .Get<SwaggerConfigurationSection>();

        ArgumentNullException.ThrowIfNull(swaggerConfiguration, nameof(swaggerConfiguration));

        if (!swaggerConfiguration.Enabled == true)
        {
            return app;
        }

        var serviceProvider = app.ApplicationServices;
        var provider = serviceProvider.GetService<IApiVersionDescriptionProvider>();
        ArgumentNullException.ThrowIfNull(provider);

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            app.UseSwaggerUI(
                options =>
                {
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerEndpoint(
                            $"./{description.GroupName}/swagger.json",
                            description.GroupName.ToUpperInvariant());
                    }
                });
        });
        return app;
    }
}
