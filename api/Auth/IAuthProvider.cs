using Api.Models;
using Microsoft.Azure.Functions.Worker.Http;

namespace Api.Auth;

/// <summary>
/// Abstraction for authentication operations.
/// Allows swapping between mock auth (demo) and SWA auth (production).
/// </summary>
public interface IAuthProvider
{
    /// <summary>
    /// Gets the current authentication mode.
    /// </summary>
    string Mode { get; }

    /// <summary>
    /// Gets whether this provider uses mock authentication.
    /// </summary>
    bool IsMockAuth { get; }

    /// <summary>
    /// Gets the login URL for this auth mode.
    /// </summary>
    string LoginUrl { get; }

    /// <summary>
    /// Gets the logout URL for this auth mode.
    /// </summary>
    string LogoutUrl { get; }

    /// <summary>
    /// Gets the current authenticated user from the request.
    /// Returns null if not authenticated.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The authenticated user info, or null.</returns>
    Task<UserInfo?> GetCurrentUserAsync(HttpRequestData request);

    /// <summary>
    /// Validates if the request is authenticated.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>True if authenticated, false otherwise.</returns>
    Task<bool> IsAuthenticatedAsync(HttpRequestData request);
}
