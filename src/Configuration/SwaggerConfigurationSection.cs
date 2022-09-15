namespace NKZSoft.Service.Configuration.Swagger.Configuration;

public class SwaggerConfigurationSection
{
    public const string SectionName = "Swagger";

    public bool? Enabled { get; set; }

    public bool? AuthorizationEnabled { get; set; }
}
