using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Publisher.Core.Enums;
using Publisher.Infrastructure;
using Publisher.Infrastructure.Data;
using Publisher.Web.Components;
using Publisher.Web.Endpoints;
using Publisher.Web.Services;
using Serilog;

// Bootstrap logger so failures before host build are still captured.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog: console + daily rolling file. Never log secrets/connection strings.
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 31));

    // Blazor Web App (InteractiveServer).
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Core/Infrastructure: AppDbContext, encryptor, audit log service.
    builder.Services.AddInfrastructure(builder.Configuration);

    // Cookie authentication.
    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/access-denied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", p => p.RequireRole(UserRoles.Admin));
    });

    // Make auth state available to components.
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddHttpContextAccessor();

    // Toast notifications (per-circuit).
    builder.Services.AddScoped<ToastService>();

    var app = builder.Build();

    // Apply migrations + idempotent seed on startup so first-run is deterministic.
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            await DbInitializer.SeedAsync(db);
            Log.Information("Database migrated and seeded successfully.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database migration/seed failed on startup.");
            throw;
        }
    }

    // HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();

    app.UseStaticFiles();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    // Authentication minimal-API endpoints (run on real HttpContext).
    app.MapAuthEndpoints();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
