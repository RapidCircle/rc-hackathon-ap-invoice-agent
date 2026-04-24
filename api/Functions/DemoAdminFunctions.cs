using System.Net;
using System.Text.Json;
using Api.Auth;
using Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api.Functions;

/// <summary>
/// Admin functions for seeding and resetting demo data.
/// Only available in development/demo environments.
/// </summary>
public class DemoAdminFunctions
{
    private readonly SampleDataSeeder _seeder;
    private readonly AppDataSeeder _appDataSeeder;
    private readonly IAuthProvider _authProvider;
    private readonly ILogger<DemoAdminFunctions> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DemoAdminFunctions(SampleDataSeeder seeder, AppDataSeeder appDataSeeder, IAuthProvider authProvider, ILogger<DemoAdminFunctions> logger)
    {
        _seeder = seeder;
        _appDataSeeder = appDataSeeder;
        _authProvider = authProvider;
        _logger = logger;
    }

    /// <summary>
    /// Seeds demo data (users/personas, vendors, invoices).
    /// Only works in non-production environments.
    /// </summary>
    [Function("DemoSeed")]
    public async Task<HttpResponseData> SeedData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "demo/seed")] HttpRequestData req)
    {
        // Block in production
        var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
        if (environment == "Production")
        {
            _logger.LogWarning("Attempt to seed data in production environment");
            var errorResponse = req.CreateResponse(HttpStatusCode.NotFound);
            return errorResponse;
        }

        if (!_authProvider.IsMockAuth)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest,
                "Demo seeding only available in mock auth mode");
        }

        try
        {
            await _seeder.SeedAsync();
            await _appDataSeeder.SeedAsync();
            _logger.LogInformation("Demo data seeded successfully");

            return await CreateJsonResponse(req, HttpStatusCode.OK, new
            {
                success = true,
                message = "Demo data seeded successfully (vendors, invoices, users)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed demo data");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError,
                "Failed to seed demo data: " + ex.Message);
        }
    }

    /// <summary>
    /// Resets all demo data.
    /// Only works in non-production environments.
    /// </summary>
    [Function("DemoReset")]
    public async Task<HttpResponseData> ResetData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "demo/reset")] HttpRequestData req)
    {
        // Block in production
        var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
        if (environment == "Production")
        {
            _logger.LogWarning("Attempt to reset data in production environment");
            var errorResponse = req.CreateResponse(HttpStatusCode.NotFound);
            return errorResponse;
        }

        if (!_authProvider.IsMockAuth)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest,
                "Demo reset only available in mock auth mode");
        }

        try
        {
            await _seeder.ResetAsync();
            await _appDataSeeder.ResetAsync();
            _logger.LogInformation("Demo data reset successfully");

            return await CreateJsonResponse(req, HttpStatusCode.OK, new
            {
                success = true,
                message = "Demo data reset and re-seeded successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset demo data");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError,
                "Failed to reset demo data: " + ex.Message);
        }
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
