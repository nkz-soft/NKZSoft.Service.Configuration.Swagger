namespace NKZSoft.Service.Configuration.Swagger.Configuration;

internal sealed record SwaggerConfigurationSection
{
    public const string SectionName = "Swagger";

    public bool? Enabled { get; set; }

    public bool? AuthorizationEnabled { get; set; }
}
