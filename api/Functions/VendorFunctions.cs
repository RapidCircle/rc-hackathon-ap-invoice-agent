using System.Net;
using System.Text.Json;
using Api.Auth;
using Api.Models;
using Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api.Functions;

/// <summary>
/// HTTP trigger functions for vendor CRUD operations.
/// </summary>
public class VendorFunctions
{
    private readonly VendorService _vendorService;
    private readonly IAuthProvider _authProvider;
    private readonly ILogger<VendorFunctions> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public VendorFunctions(VendorService vendorService, IAuthProvider authProvider, ILogger<VendorFunctions> logger)
    {
        _vendorService = vendorService;
        _authProvider = authProvider;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/vendors — List all active vendors.
    /// </summary>
    [Function("VendorList")]
    public async Task<HttpResponseData> ListVendors(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vendors")] HttpRequestData req)
    {
        try
        {
            var vendors = await _vendorService.ListAsync();
            return await CreateJsonResponse(req, HttpStatusCode.OK, vendors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list vendors");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to list vendors.");
        }
    }

    /// <summary>
    /// POST /api/vendors — Create a new vendor.
    /// </summary>
    [Function("VendorCreate")]
    public async Task<HttpResponseData> CreateVendor(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "vendors")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required.");
            }

            var request = JsonSerializer.Deserialize<VendorCreateRequest>(body, JsonOptions);
            if (request == null || string.IsNullOrWhiteSpace(request.LegalName))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Vendor legal name is required.");
            }

            // Check for duplicate legal name
            var existing = await _vendorService.FindByLegalNameAsync(request.LegalName);
            if (existing != null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.Conflict,
                    $"Vendor with legal name '{request.LegalName}' already exists.");
            }

            var vendor = new VendorEntity
            {
                LegalName = request.LegalName,
                TradingName = request.TradingName,
                TaxId = request.TaxId
            };

            var created = await _vendorService.CreateAsync(vendor);
            return await CreateJsonResponse(req, HttpStatusCode.Created, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create vendor");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to create vendor.");
        }
    }

    /// <summary>
    /// PUT /api/vendors/{id} — Update a vendor.
    /// </summary>
    [Function("VendorUpdate")]
    public async Task<HttpResponseData> UpdateVendor(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "vendors/{id}")] HttpRequestData req,
        string id)
    {
        try
        {
            var vendor = await _vendorService.GetByIdAsync(id);
            if (vendor == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, $"Vendor '{id}' not found.");
            }

            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required.");
            }

            var request = JsonSerializer.Deserialize<VendorUpdateRequest>(body, JsonOptions);
            if (request == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body.");
            }

            if (request.LegalName != null) vendor.LegalName = request.LegalName;
            if (request.TradingName != null) vendor.TradingName = request.TradingName;
            if (request.TaxId != null) vendor.TaxId = request.TaxId;
            if (request.IsActive.HasValue) vendor.IsActive = request.IsActive.Value;

            var updated = await _vendorService.UpdateAsync(vendor);
            return await CreateJsonResponse(req, HttpStatusCode.OK, updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update vendor {VendorId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to update vendor.");
        }
    }

    /// <summary>
    /// DELETE /api/vendors/{id} — Soft-delete a vendor (sets IsActive=false).
    /// </summary>
    [Function("VendorDelete")]
    public async Task<HttpResponseData> DeleteVendor(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "vendors/{id}")] HttpRequestData req,
        string id)
    {
        try
        {
            var vendor = await _vendorService.DeleteAsync(id);
            if (vendor == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, $"Vendor '{id}' not found.");
            }

            return await CreateJsonResponse(req, HttpStatusCode.OK, new
            {
                success = true,
                message = $"Vendor '{vendor.LegalName}' has been deactivated."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete vendor {VendorId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to delete vendor.");
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
