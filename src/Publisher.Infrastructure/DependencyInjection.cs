using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Publisher.Core.Interfaces;
using Publisher.Core.Options;
using Publisher.Infrastructure.AI;
using Publisher.Infrastructure.Auditing;
using Publisher.Infrastructure.Data;
using Publisher.Infrastructure.Publishing;
using Publisher.Infrastructure.Security;
using Publisher.Infrastructure.Sites;

namespace Publisher.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the infrastructure layer:
    /// - EncryptionOptions (env var PUBLISHER_ENCRYPTION_KEY preferred, else config "Encryption:Key")
    /// - AppDbContext (SqlServer, connection string "AppDb")
    /// - IConnectionStringEncryptor (singleton)
    /// - IAuditLogService (scoped)
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Encryption key: env var takes precedence over config.
        var envKey = Environment.GetEnvironmentVariable("PUBLISHER_ENCRYPTION_KEY");
        var configKey = config.GetSection(EncryptionOptions.SectionName)["Key"];
        var resolvedKey = !string.IsNullOrWhiteSpace(envKey) ? envKey : configKey;

        services.Configure<EncryptionOptions>(opts =>
        {
            opts.Key = resolvedKey ?? string.Empty;
        });

        // AppDB context.
        var appDbConnectionString = config.GetConnectionString("AppDb");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(appDbConnectionString, sql =>
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddSingleton<IConnectionStringEncryptor, ConnectionStringEncryptor>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // Agent C: remote-DB connector + parameterized INSERT builder.
        services.AddScoped<ISiteDbConnector, SiteDbConnector>();
        services.AddSingleton<InsertCommandBuilder>();

        // Agent E: post publisher (writes posts into the target site DB via the INSERT builder).
        services.AddScoped<IPostPublisher, PostPublisher>();

        // Phase 4: AI Gateway client. Env vars take precedence over config.
        var aiUrl = Environment.GetEnvironmentVariable("PUBLISHER_AIGATEWAY_URL");
        var aiKey = Environment.GetEnvironmentVariable("PUBLISHER_AIGATEWAY_KEY");
        services.Configure<AIGatewayOptions>(opts =>
        {
            config.GetSection(AIGatewayOptions.SectionName).Bind(opts);
            if (!string.IsNullOrWhiteSpace(aiUrl)) opts.BaseUrl = aiUrl;
            if (!string.IsNullOrWhiteSpace(aiKey)) opts.ApiKey = aiKey;
        });

        services.AddHttpClient<IAIContentService, AIContentService>((sp, client) =>
        {
            var o = sp.GetRequiredService<IOptions<AIGatewayOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(o.BaseUrl))
            {
                var baseUrl = o.BaseUrl.EndsWith('/') ? o.BaseUrl : o.BaseUrl + "/";
                client.BaseAddress = new Uri(baseUrl);
            }
            client.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds > 0 ? o.TimeoutSeconds : 120);
        });

        return services;
    }
}
