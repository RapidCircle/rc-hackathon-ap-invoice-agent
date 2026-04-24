using System.Text.Json;
using Api.Models;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api.Auth;

/// <summary>
/// Mock authentication provider for demo purposes.
/// Uses Azure Table Storage for session management.
/// </summary>
public class MockAuthProvider : IAuthProvider
{
    private readonly TableClient _sessionTable;
    private readonly TableClient _userTable;
    private readonly ILogger<MockAuthProvider> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    public string Mode => "mock";

    /// <inheritdoc />
    public bool IsMockAuth => true;

    /// <inheritdoc />
    public string LoginUrl => "/login.html";

    /// <inheritdoc />
    public string LogoutUrl => "/api/auth/logout";

    public MockAuthProvider(TableServiceClient tableServiceClient, ILogger<MockAuthProvider> logger)
    {
        _sessionTable = tableServiceClient.GetTableClient("AuthSessions");
        _userTable = tableServiceClient.GetTableClient("Users");
        _logger = logger;
    }

    /// <summary>
    /// Ensures required tables exist.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _sessionTable.CreateIfNotExistsAsync();
        await _userTable.CreateIfNotExistsAsync();
    }

    /// <inheritdoc />
    public async Task<UserInfo?> GetCurrentUserAsync(HttpRequestData request)
    {
        var sessionToken = GetSessionToken(request);
        if (string.IsNullOrEmpty(sessionToken))
        {
            return null;
        }

        var session = await GetValidSessionAsync(sessionToken);
        if (session == null || !session.MfaCompleted)
        {
            return null;
        }

        var user = await GetUserByIdAsync(session.UserId);
        if (user == null)
        {
            return null;
        }

        return MapToUserInfo(user);
    }

    /// <inheritdoc />
    public async Task<bool> IsAuthenticatedAsync(HttpRequestData request)
    {
        var user = await GetCurrentUserAsync(request);
        return user != null;
    }

    /// <summary>
    /// Initiates login for a user by email.
    /// Creates a temporary session requiring MFA.
    /// </summary>
    public async Task<LoginResponse> LoginAsync(string email)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt for unknown email: {Email}", email);
            return new LoginResponse { Error = "User not found" };
        }

        if (user.Status != "Active")
        {
            _logger.LogWarning("Login attempt for inactive user: {Email}", email);
            return new LoginResponse { Error = "Account is not active" };
        }

        // Create temporary session (5 min TTL, MFA not completed)
        var session = new AuthSession
        {
            SessionId = Guid.NewGuid().ToString(),
            RowKey = Guid.NewGuid().ToString(),
            UserId = user.UserId,
            Email = user.Email,
            MfaCompleted = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        };

        await _sessionTable.UpsertEntityAsync(session);
        _logger.LogInformation("Created temp session for user: {Email}", email);

        return new LoginResponse
        {
            SessionToken = session.SessionId,
            RequiresMfa = true
        };
    }

    /// <summary>
    /// Verifies MFA code and completes authentication.
    /// Fixed code: 123456
    /// </summary>
    public async Task<MfaResponse> VerifyMfaAsync(string sessionToken, string code)
    {
        const string validCode = "123456";

        var session = await GetSessionByTokenAsync(sessionToken);
        if (session == null)
        {
            return new MfaResponse { Success = false, Error = "Session not found or expired" };
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            await _sessionTable.DeleteEntityAsync(session.PartitionKey, session.RowKey);
            return new MfaResponse { Success = false, Error = "Session expired" };
        }

        if (code != validCode)
        {
            _logger.LogWarning("Invalid MFA code for session: {SessionId}", sessionToken);
            return new MfaResponse { Success = false, Error = "Invalid verification code" };
        }

        // Extend session (8 hour TTL) and mark MFA complete
        session.MfaCompleted = true;
        session.ExpiresAt = DateTime.UtcNow.AddHours(8);
        await _sessionTable.UpsertEntityAsync(session);

        var user = await GetUserByIdAsync(session.UserId);
        if (user == null)
        {
            return new MfaResponse { Success = false, Error = "User not found" };
        }

        _logger.LogInformation("MFA verified for user: {Email}", user.Email);

        return new MfaResponse
        {
            Success = true,
            SessionToken = session.SessionId,
            User = MapToUserInfo(user)
        };
    }

    /// <summary>
    /// Invalidates a session (logout).
    /// </summary>
    public async Task LogoutAsync(string sessionToken)
    {
        var session = await GetSessionByTokenAsync(sessionToken);
        if (session != null)
        {
            await _sessionTable.DeleteEntityAsync(session.PartitionKey, session.RowKey);
            _logger.LogInformation("Session invalidated: {SessionId}", sessionToken);
        }
    }

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        await foreach (var user in _userTable.QueryAsync<User>(u => u.Email == email))
        {
            return user;
        }
        return null;
    }

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    public async Task<User?> GetUserByIdAsync(string userId)
    {
        try
        {
            var response = await _userTable.GetEntityAsync<User>("User", userId);
            return response.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all demo users.
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = new List<User>();
        await foreach (var user in _userTable.QueryAsync<User>())
        {
            users.Add(user);
        }
        return users;
    }

    /// <summary>
    /// Saves a user to the database.
    /// </summary>
    public async Task SaveUserAsync(User user)
    {
        user.RowKey = user.UserId;
        await _userTable.UpsertEntityAsync(user);
    }

    /// <summary>
    /// Clears all users and sessions (for reset).
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        // Delete all sessions
        await foreach (var session in _sessionTable.QueryAsync<AuthSession>())
        {
            await _sessionTable.DeleteEntityAsync(session.PartitionKey, session.RowKey);
        }

        // Delete all users
        await foreach (var user in _userTable.QueryAsync<User>())
        {
            await _userTable.DeleteEntityAsync(user.PartitionKey, user.RowKey);
        }

        _logger.LogInformation("Cleared all auth data");
    }

    private static string? GetSessionToken(HttpRequestData request)
    {
        if (request.Headers.TryGetValues("X-Session-Token", out var values))
        {
            return values.FirstOrDefault();
        }
        return null;
    }

    private async Task<AuthSession?> GetSessionByTokenAsync(string sessionToken)
    {
        await foreach (var session in _sessionTable.QueryAsync<AuthSession>(s => s.SessionId == sessionToken))
        {
            return session;
        }
        return null;
    }

    private async Task<AuthSession?> GetValidSessionAsync(string sessionToken)
    {
        var session = await GetSessionByTokenAsync(sessionToken);
        if (session == null || session.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }
        return session;
    }

    private static UserInfo MapToUserInfo(User user)
    {
        var roles = user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        return new UserInfo
        {
            IdentityProvider = "mock",
            UserId = user.UserId,
            UserDetails = user.Email,
            UserRoles = roles.Length > 0 ? roles : ["anonymous", "authenticated"],
            FullName = user.FullName,
            Email = user.Email,
            IsSystemAdmin = user.IsSystemAdmin,
            PersonaDescription = user.PersonaDescription
        };
    }
}
