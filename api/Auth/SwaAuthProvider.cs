using System.Text;
using System.Text.Json;
using Api.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api.Auth;

/// <summary>
/// SWA (Static Web Apps) authentication provider for production.
/// Reads the x-ms-client-principal header set by Azure Static Web Apps.
/// </summary>
public class SwaAuthProvider : IAuthProvider
{
    private readonly ILogger<SwaAuthProvider> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public string Mode => "swa";

    /// <inheritdoc />
    public bool IsMockAuth => false;

    /// <inheritdoc />
    public string LoginUrl => "/.auth/login/aad";

    /// <inheritdoc />
    public string LogoutUrl => "/.auth/logout";

    public SwaAuthProvider(ILogger<SwaAuthProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<UserInfo?> GetCurrentUserAsync(HttpRequestData request)
    {
        var clientPrincipal = ParseClientPrincipal(request);
        if (clientPrincipal == null)
        {
            return Task.FromResult<UserInfo?>(null);
        }

        var userInfo = new UserInfo
        {
            IdentityProvider = clientPrincipal.IdentityProvider ?? "aad",
            UserId = clientPrincipal.UserId ?? "",
            UserDetails = clientPrincipal.UserDetails ?? "",
            UserRoles = clientPrincipal.UserRoles ?? ["anonymous", "authenticated"],
            FullName = clientPrincipal.UserDetails ?? "",
            Email = clientPrincipal.UserDetails ?? "",
            IsSystemAdmin = false,
            PersonaDescription = ""
        };

        return Task.FromResult<UserInfo?>(userInfo);
    }

    /// <inheritdoc />
    public Task<bool> IsAuthenticatedAsync(HttpRequestData request)
    {
        var principal = ParseClientPrincipal(request);
        var isAuthenticated = principal != null && 
                              principal.UserRoles != null && 
                              principal.UserRoles.Contains("authenticated");
        return Task.FromResult(isAuthenticated);
    }

    private ClientPrincipal? ParseClientPrincipal(HttpRequestData request)
    {
        if (!request.Headers.TryGetValues("x-ms-client-principal", out var values))
        {
            return null;
        }

        var header = values.FirstOrDefault();
        if (string.IsNullOrEmpty(header))
        {
            return null;
        }

        try
        {
            var decoded = Convert.FromBase64String(header);
            var json = Encoding.UTF8.GetString(decoded);
            return JsonSerializer.Deserialize<ClientPrincipal>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse client principal header");
            return null;
        }
    }

    /// <summary>
    /// Represents the SWA client principal structure.
    /// </summary>
    private class ClientPrincipal
    {
        public string? IdentityProvider { get; set; }
        public string? UserId { get; set; }
        public string? UserDetails { get; set; }
        public string[]? UserRoles { get; set; }
    }
}
