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
/// HTTP trigger functions for batch operations — creating and pushing invoice batches to Zoho.
/// </summary>
public class BatchFunctions
{
    private readonly BatchService _batchService;
    private readonly IAuthProvider _authProvider;
    private readonly ILogger<BatchFunctions> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BatchFunctions(BatchService batchService, IAuthProvider authProvider, ILogger<BatchFunctions> logger)
    {
        _batchService = batchService;
        _authProvider = authProvider;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/batches — List all batches.
    /// </summary>
    [Function("BatchList")]
    public async Task<HttpResponseData> ListBatches(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "batches")] HttpRequestData req)
    {
        try
        {
            var batches = await _batchService.ListBatchesAsync();
            return await CreateJsonResponse(req, HttpStatusCode.OK, batches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list batches");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to list batches.");
        }
    }

    /// <summary>
    /// POST /api/batches — Create a new batch from ReadyForZoho invoices.
    /// </summary>
    [Function("BatchCreate")]
    public async Task<HttpResponseData> CreateBatch(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "batches")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required.");
            }

            var request = JsonSerializer.Deserialize<BatchCreateRequest>(body, JsonOptions);
            if (request == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body.");
            }

            var batch = await _batchService.CreateBatchAsync(request.CutoffDateTime);

            if (batch.InvoiceCount == 0)
            {
                return await CreateJsonResponse(req, HttpStatusCode.OK, new
                {
                    success = true,
                    message = "Batch created but no eligible invoices found before the cutoff date.",
                    batch
                });
            }

            return await CreateJsonResponse(req, HttpStatusCode.Created, batch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create batch");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to create batch.");
        }
    }

    /// <summary>
    /// POST /api/batches/{id}/push — Push a batch to Zoho (mock).
    /// </summary>
    [Function("BatchPush")]
    public async Task<HttpResponseData> PushBatch(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "batches/{id}/push")] HttpRequestData req,
        string id)
    {
        try
        {
            var batch = await _batchService.PushBatchAsync(id);
            if (batch == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, $"Batch '{id}' not found.");
            }

            if (batch.Status == BatchStatus.Failed)
            {
                return await CreateJsonResponse(req, HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Batch push failed.",
                    batch
                });
            }

            return await CreateJsonResponse(req, HttpStatusCode.OK, new
            {
                success = true,
                message = $"Batch pushed successfully. {batch.InvoiceCount} invoices sent to Zoho (mock).",
                batch
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push batch {BatchId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to push batch.");
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
