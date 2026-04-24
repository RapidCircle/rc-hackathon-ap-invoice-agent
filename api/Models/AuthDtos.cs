namespace Api.Models;

/// <summary>
/// Request to initiate login with email.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// The user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Response after initiating login.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Temporary session token for MFA verification.
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether MFA is required to complete login.
    /// </summary>
    public bool RequiresMfa { get; set; } = true;

    /// <summary>
    /// Error message if login failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Request to verify MFA code.
/// </summary>
public class MfaRequest
{
    /// <summary>
    /// The session token from the login response.
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// The MFA verification code (use 123456 for demo).
    /// </summary>
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Response after successful MFA verification.
/// </summary>
public class MfaResponse
{
    /// <summary>
    /// Whether authentication was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The session token to use for subsequent requests.
    /// </summary>
    public string? SessionToken { get; set; }

    /// <summary>
    /// The authenticated user's information.
    /// </summary>
    public UserInfo? User { get; set; }

    /// <summary>
    /// Error message if MFA failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// User information returned to the frontend.
/// Matches the shape of SWA's clientPrincipal for compatibility.
/// </summary>
public class UserInfo
{
    /// <summary>
    /// The identity provider (always "mock" for demo, "aad" for production).
    /// </summary>
    public string IdentityProvider { get; set; } = "mock";

    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The user's display details (typically email or name).
    /// </summary>
    public string UserDetails { get; set; } = string.Empty;

    /// <summary>
    /// The user's roles.
    /// </summary>
    public string[] UserRoles { get; set; } = ["anonymous", "authenticated"];

    /// <summary>
    /// The user's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user is a system administrator.
    /// </summary>
    public bool IsSystemAdmin { get; set; }

    /// <summary>
    /// User's persona description (for demo purposes).
    /// </summary>
    public string PersonaDescription { get; set; } = string.Empty;
}

/// <summary>
/// Response from the /auth/me endpoint.
/// </summary>
public class AuthMeResponse
{
    /// <summary>
    /// Whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// The current auth mode: "mock" or "swa".
    /// </summary>
    public string AuthMode { get; set; } = "mock";

    /// <summary>
    /// The authenticated user's information, if authenticated.
    /// </summary>
    public UserInfo? User { get; set; }
}

/// <summary>
/// Response from the /auth/mode endpoint.
/// </summary>
public class AuthModeResponse
{
    /// <summary>
    /// The current authentication mode: "mock" or "swa".
    /// </summary>
    public string Mode { get; set; } = "mock";

    /// <summary>
    /// The login URL to use for this mode.
    /// </summary>
    public string LoginUrl { get; set; } = "/login.html";

    /// <summary>
    /// The logout URL to use for this mode.
    /// </summary>
    public string LogoutUrl { get; set; } = "/api/auth/logout";
}
