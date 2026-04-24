using System.Net;
using System.Text.Json;
using Api.Auth;
using Api.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api.Functions;

/// <summary>
/// Mock authentication API endpoints.
/// These endpoints are only available when mock authentication is enabled.
/// 
/// DELETE THIS FILE to remove mock authentication endpoints from the application.
/// When this file is deleted, these API routes will return 404.
/// </summary>
public class MockAuthFunctions
{
    private readonly MockAuthProvider _mockAuthProvider;
    private readonly ILogger<MockAuthFunctions> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MockAuthFunctions(MockAuthProvider mockAuthProvider, ILogger<MockAuthFunctions> logger)
    {
        _mockAuthProvider = mockAuthProvider;
        _logger = logger;
    }

    /// <summary>
    /// Initiates login with email.
    /// Returns a session token that requires MFA verification.
    /// </summary>
    [Function("MockAuthLogin")]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req)
    {
        var loginRequest = await req.ReadFromJsonAsync<LoginRequest>();
        if (loginRequest == null || string.IsNullOrWhiteSpace(loginRequest.Email))
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Email is required");
        }

        var result = await _mockAuthProvider.LoginAsync(loginRequest.Email.Trim().ToLowerInvariant());

        if (result.Error != null)
        {
            return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, result.Error);
        }

        return await CreateJsonResponse(req, HttpStatusCode.OK, result);
    }

    /// <summary>
    /// Verifies MFA code and completes authentication.
    /// Use code: 123456
    /// </summary>
    [Function("MockAuthMfa")]
    public async Task<HttpResponseData> VerifyMfa(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/mfa")] HttpRequestData req)
    {
        var mfaRequest = await req.ReadFromJsonAsync<MfaRequest>();
        if (mfaRequest == null || string.IsNullOrWhiteSpace(mfaRequest.SessionToken))
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Session token is required");
        }

        if (string.IsNullOrWhiteSpace(mfaRequest.Code))
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Verification code is required");
        }

        var result = await _mockAuthProvider.VerifyMfaAsync(mfaRequest.SessionToken, mfaRequest.Code);

        if (!result.Success)
        {
            return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, result.Error ?? "MFA verification failed");
        }

        return await CreateJsonResponse(req, HttpStatusCode.OK, result);
    }

    /// <summary>
    /// Logs out the current user by invalidating the session.
    /// </summary>
    [Function("MockAuthLogout")]
    public async Task<HttpResponseData> Logout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/logout")] HttpRequestData req)
    {
        var sessionToken = GetSessionToken(req);
        if (!string.IsNullOrEmpty(sessionToken))
        {
            await _mockAuthProvider.LogoutAsync(sessionToken);
        }

        return await CreateJsonResponse(req, HttpStatusCode.OK, new { success = true });
    }

    /// <summary>
    /// Returns the list of available demo personas for easy login.
    /// </summary>
    [Function("MockAuthPersonas")]
    public async Task<HttpResponseData> GetPersonas(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/personas")] HttpRequestData req)
    {
        var users = await _mockAuthProvider.GetAllUsersAsync();
        var personas = users.Select(u => new
        {
            email = u.Email,
            fullName = u.FullName,
            description = u.PersonaDescription,
            isAdmin = u.IsSystemAdmin
        }).ToList();

        return await CreateJsonResponse(req, HttpStatusCode.OK, new { personas });
    }

    private static string? GetSessionToken(HttpRequestData req)
    {
        if (req.Headers.TryGetValues("X-Session-Token", out var values))
        {
            return values.FirstOrDefault();
        }
        return null;
    }

    private static async Task<HttpResponseData> CreateJsonResponse<T>(HttpRequestData req, HttpStatusCode statusCode, T data)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(data, JsonOptions));
        return response;
    }

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        return await CreateJsonResponse(req, statusCode, new { error = message });
    }
}
