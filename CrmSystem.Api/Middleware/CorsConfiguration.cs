namespace CrmSystem.Api.Middleware;

/// <summary>
/// CORS 配置扩展方法
/// </summary>
public static class CorsConfiguration
{
    public const string CorsPolicy = "CrmCorsPolicy";

    /// <summary>
    /// 配置 CORS 服务
    /// </summary>
    public static IServiceCollection AddCrmCors(this IServiceCollection services, IConfiguration configuration)
    {
        // Normalize origins from configuration and allow common comma/semicolon separated formats
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? configuration["Cors:AllowedOrigins"]?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            ?? Array.Empty<string>();

        var allowedOrigins = configuredOrigins
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim().TrimEnd('/')) // normalize so http://localhost:3000/ works
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (allowedOrigins.Length == 0)
        {
            allowedOrigins = new[] { "http://localhost:3000", "http://localhost:5173" };
        }

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy, builder =>
            {
                builder
                    .SetIsOriginAllowed(origin =>
                    {
                        if (string.IsNullOrWhiteSpace(origin))
                            return false;

                        var normalized = origin.Trim().TrimEnd('/');
                        return allowedOrigins.Any(o => 
                            string.Equals(o, normalized, StringComparison.OrdinalIgnoreCase));
                    })
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("ETag", "Location")
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// 使用 CORS 中间件
    /// </summary>
    public static IApplicationBuilder UseCrmCors(this IApplicationBuilder app)
    {
        return app.UseCors(CorsPolicy);
    }
}
