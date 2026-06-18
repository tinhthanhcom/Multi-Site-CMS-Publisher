using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Publisher.Core.Interfaces;
using Publisher.Infrastructure.Data;

namespace Publisher.Web.Endpoints;

/// <summary>
/// Minimal-API authentication endpoints. These run over a real HttpContext (NOT a
/// SignalR circuit), which is required because Blazor InteractiveServer components
/// cannot call SignInAsync/SignOutAsync. Login/change-password pages post plain
/// HTML forms (static SSR) to these endpoints.
/// </summary>
public static class AuthEndpoints
{
    public const string FullNameClaim = "FullName";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/login", LoginAsync);
        group.MapPost("/logout", LogoutAsync);
        group.MapPost("/change-password", ChangePasswordAsync);

        return app;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext http,
        [FromForm] string? username,
        [FromForm] string? password,
        [FromForm] string? returnUrl,
        AppDbContext db,
        IAuditLogService audit,
        IAntiforgery antiforgery,
        ILogger<LoginLog> logger,
        CancellationToken ct)
    {
        await antiforgery.ValidateRequestAsync(http);

        var ip = http.Connection.RemoteIpAddress?.ToString();
        username = (username ?? string.Empty).Trim();

        // NOTE: never log the password.
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await audit.WriteAsync("LOGIN", entityType: "User", entityId: username,
                isSuccess: false, errorMessage: "Missing credentials", ipAddress: ip, ct: ct);
            return Results.Redirect("/login?error=1");
        }

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);

        var ok = user is not null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (!ok || user is null)
        {
            logger.LogWarning("Failed login attempt for username {Username}", username);
            await audit.WriteAsync("LOGIN", userId: user?.Id, entityType: "User", entityId: username,
                isSuccess: false, errorMessage: "Invalid credentials", ipAddress: ip, ct: ct);
            return Results.Redirect("/login?error=1");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(FullNameClaim, user.FullName)
        };
        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User {Username} logged in", user.Username);
        await audit.WriteAsync("LOGIN", userId: user.Id, entityType: "User", entityId: user.Id.ToString(),
            isSuccess: true, ipAddress: ip, ct: ct);

        return Results.Redirect(SafeRedirect(returnUrl));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext http,
        IAuditLogService audit,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        await antiforgery.ValidateRequestAsync(http);

        int? userId = ParseUserId(http.User);
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        await audit.WriteAsync("LOGOUT", userId: userId, entityType: "User", entityId: userId?.ToString(),
            isSuccess: true, ipAddress: http.Connection.RemoteIpAddress?.ToString(), ct: ct);

        return Results.Redirect("/login");
    }

    private static async Task<IResult> ChangePasswordAsync(
        HttpContext http,
        [FromForm] string? currentPassword,
        [FromForm] string? newPassword,
        [FromForm] string? confirmPassword,
        AppDbContext db,
        IAuditLogService audit,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        await antiforgery.ValidateRequestAsync(http);

        if (http.User?.Identity?.IsAuthenticated != true)
            return Results.Redirect("/login");

        var userId = ParseUserId(http.User);
        if (userId is null)
            return Results.Redirect("/login");

        var ip = http.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) ||
            newPassword.Length < 6 || newPassword != confirmPassword)
        {
            return Results.Redirect("/account/change-password?error=1");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            await audit.WriteAsync("PASSWORD_CHANGED", userId: userId, entityType: "User",
                entityId: userId?.ToString(), isSuccess: false, errorMessage: "Current password mismatch",
                ipAddress: ip, ct: ct);
            return Results.Redirect("/account/change-password?error=2");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync("PASSWORD_CHANGED", userId: user.Id, entityType: "User",
            entityId: user.Id.ToString(), isSuccess: true, ipAddress: ip, ct: ct);

        return Results.Redirect("/account/change-password?success=1");
    }

    private static int? ParseUserId(ClaimsPrincipal? principal)
    {
        var raw = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    /// <summary>Only allow local relative redirects to avoid open-redirect.</summary>
    private static string SafeRedirect(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) &&
            Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) &&
            returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
        {
            return returnUrl;
        }
        return "/";
    }

    /// <summary>Marker type for a category-specific logger.</summary>
    public sealed class LoginLog { }
}
