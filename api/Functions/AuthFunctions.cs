using System.Net;
using System.Text.Json;
using Api.Auth;
using Api.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api.Functions;

/// <summary>
/// Universal authentication API endpoints.
/// These endpoints work for both mock and SWA authentication modes.
/// </summary>
public class AuthFunctions
{
    private readonly IAuthProvider _authProvider;
    private readonly ILogger<AuthFunctions> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuthFunctions(IAuthProvider authProvider, ILogger<AuthFunctions> logger)
    {
        _authProvider = authProvider;
        _logger = logger;
    }

    /// <summary>
    /// Returns the current authentication mode and URLs.
    /// Always accessible, regardless of auth mode.
    /// </summary>
    [Function("AuthMode")]
    public async Task<HttpResponseData> GetAuthMode(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/mode")] HttpRequestData req)
    {
        var response = new AuthModeResponse
        {
            Mode = _authProvider.Mode,
            LoginUrl = _authProvider.LoginUrl,
            LogoutUrl = _authProvider.LogoutUrl
        };

        return await CreateJsonResponse(req, HttpStatusCode.OK, response);
    }

    /// <summary>
    /// Returns the current authenticated user.
    /// Works for both mock and SWA auth modes.
    /// </summary>
    [Function("AuthMe")]
    public async Task<HttpResponseData> GetCurrentUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/me")] HttpRequestData req)
    {
        var user = await _authProvider.GetCurrentUserAsync(req);

        var response = new AuthMeResponse
        {
            IsAuthenticated = user != null,
            AuthMode = _authProvider.Mode,
            User = user
        };

        return await CreateJsonResponse(req, HttpStatusCode.OK, response);
    }

    private static async Task<HttpResponseData> CreateJsonResponse<T>(HttpRequestData req, HttpStatusCode statusCode, T data)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(data, JsonOptions));
        return response;
    }
}
