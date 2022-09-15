namespace NKZSoft.Service.Configuration.Swagger;

using Configuration;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSwagger(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var swaggerConfiguration = configuration.GetSection(SwaggerConfigurationSection.SectionName)
            .Get<SwaggerConfigurationSection>();

        if (!swaggerConfiguration.Enabled == true)
        {
            return app;
        }

        var serviceProvider = app.ApplicationServices;
        var provider = serviceProvider.GetService<IApiVersionDescriptionProvider>();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            app.UseSwaggerUI(
                options =>
                {
                    //Well about !
                    //I'd rather NRE here than not sure why an empty swagger
                    foreach (var description in provider!.ApiVersionDescriptions)
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
