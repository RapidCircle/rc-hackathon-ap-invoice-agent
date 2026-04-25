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
/// HTTP trigger functions for invoice CRUD operations and validation.
/// </summary>
public class InvoiceFunctions
{
    private readonly InvoiceService _invoiceService;
    private readonly ValidationService _validationService;
    private readonly ExtractionService _extractionService;
    private readonly IAuthProvider _authProvider;
    private readonly ILogger<InvoiceFunctions> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InvoiceFunctions(
        InvoiceService invoiceService,
        ValidationService validationService,
        ExtractionService extractionService,
        IAuthProvider authProvider,
        ILogger<InvoiceFunctions> logger)
    {
        _invoiceService = invoiceService;
        _validationService = validationService;
        _extractionService = extractionService;
        _authProvider = authProvider;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/invoices — List invoices with optional ?status= filter.
    /// </summary>
    [Function("InvoiceList")]
    public async Task<HttpResponseData> ListInvoices(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invoices")] HttpRequestData req)
    {
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var statusFilter = query["status"];

            if (statusFilter != null && !InvoiceStatus.IsValid(statusFilter))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest,
                    $"Invalid status filter '{statusFilter}'. Valid values: {string.Join(", ", InvoiceStatus.All)}");
            }

            var invoices = await _invoiceService.ListAsync(statusFilter);

            var items = invoices.Select(i => new InvoiceListItem
            {
                Id = i.RowKey,
                VendorLegalName = i.VendorLegalName,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                InvoiceCurrency = i.InvoiceCurrency,
                TotalAmount = i.TotalAmount,
                Status = i.Status,
                ExceptionReason = i.ExceptionReason,
                PoNumber = i.PoNumber,
                CreatedAt = i.CreatedAt
            }).ToList();

            return await CreateJsonResponse(req, HttpStatusCode.OK, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list invoices");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to list invoices.");
        }
    }

    /// <summary>
    /// GET /api/invoice-stats — Get dashboard statistics.
    /// Route uses "invoice-stats" instead of "invoices/stats" to avoid conflict with "invoices/{id}".
    /// </summary>
    [Function("InvoiceStats")]
    public async Task<HttpResponseData> GetStats(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invoice-stats")] HttpRequestData req)
    {
        try
        {
            var stats = await _invoiceService.GetStatsAsync();
            return await CreateJsonResponse(req, HttpStatusCode.OK, stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get invoice stats");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get invoice statistics.");
        }
    }

    /// <summary>
    /// GET /api/invoices/{id} — Get a single invoice by ID.
    /// </summary>
    [Function("InvoiceGetById")]
    public async Task<HttpResponseData> GetInvoice(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invoices/{id}")] HttpRequestData req,
        string id)
    {
        try
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, $"Invoice '{id}' not found.");
            }

            return await CreateJsonResponse(req, HttpStatusCode.OK, invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get invoice {InvoiceId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to get invoice.");
        }
    }

    /// <summary>
    /// POST /api/invoices — Create a new invoice.
    /// </summary>
    [Function("InvoiceCreate")]
    public async Task<HttpResponseData> CreateInvoice(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required.");
            }

            var request = JsonSerializer.Deserialize<InvoiceCreateRequest>(body, JsonOptions);
            if (request == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body.");
            }

            var invoice = new InvoiceEntity
            {
                VendorLegalName = request.VendorLegalName,
                InvoiceNumber = request.InvoiceNumber,
                InvoiceDate = request.InvoiceDate.HasValue
                    ? DateTime.SpecifyKind(request.InvoiceDate.Value, DateTimeKind.Utc)
                    : DateTime.UtcNow,
                InvoiceCurrency = request.Currency,
                TotalAmount = request.TotalAmount,
                TaxAmount = request.TaxAmount,
                PoNumber = request.PoNumber,
                LineItemsSummary = request.LineItemsSummary,
                EmailSender = request.EmailSender,
                EmailSubject = request.EmailSubject,
                EmailReceivedDate = DateTime.UtcNow,
                AttachmentName = request.AttachmentName,
                Status = InvoiceStatus.Received,
                CreatedBy = "api"
            };

            var created = await _invoiceService.CreateAsync(invoice);
            return await CreateJsonResponse(req, HttpStatusCode.Created, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invoice");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to create invoice.");
        }
    }

    /// <summary>
    /// PUT /api/invoices/{id} — Update an existing invoice.
    /// </summary>
    [Function("InvoiceUpdate")]
    public async Task<HttpResponseData> UpdateInvoice(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "invoices/{id}")] HttpRequestData req,
        string id)
    {
        try
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, $"Invoice '{id}' not found.");
            }

            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required.");
            }

            var request = JsonSerializer.Deserialize<InvoiceUpdateRequest>(body, JsonOptions);
            if (request == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body.");
            }

            // Apply partial updates — only update non-null fields
            if (request.VendorLegalName != null) invoice.VendorLegalName = request.VendorLegalName;
            if (request.InvoiceNumber != null) invoice.InvoiceNumber = request.InvoiceNumber;
            if (request.InvoiceDate.HasValue) invoice.InvoiceDate = DateTime.SpecifyKind(request.InvoiceDate.Value, DateTimeKind.Utc);
            if (request.Currency != null) invoice.InvoiceCurrency = request.Currency;
            if (request.TotalAmount.HasValue) invoice.TotalAmount = request.TotalAmount.Value;
            if (request.TaxAmount.HasValue) invoice.TaxAmount = request.TaxAmount.Value;
            if (request.PoNumber != null) invoice.PoNumber = request.PoNumber;
            if (request.LineItemsSummary != null) invoice.LineItemsSummary = request.LineItemsSummary;
            if (request.ExceptionReason != null) invoice.ExceptionReason = request.ExceptionReason;
            if (request.ExceptionNotes != null) invoice.ExceptionNotes = request.ExceptionNotes;

            if (request.Status != null)
            {
                if (!InvoiceStatus.IsValid(request.Status))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest,
                        $"Invalid status '{request.Status}'. Valid values: {string.Join(", ", InvoiceStatus.All)}");
                }
                invoice.Status = request.Status;
            }

            invoice.UpdatedBy = "api";
            var updated = await _invoiceService.UpdateAsync(invoice);
            return await CreateJsonResponse(req, HttpStatusCode.OK, updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update invoice {InvoiceId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to update invoice.");
        }
    }

    /// <summary>
    /// POST /api/invoices/upload — Upload invoice file and extract data using Azure Document Intelligence.
    /// </summary>
    [Function("InvoiceUpload")]
    public async Task<HttpResponseData> UploadInvoice(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices/upload")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Processing invoice upload");

            // Parse multipart form data
            var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault() ?? "";
            if (!contentType.Contains("multipart/form-data"))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, 
                    "Request must be multipart/form-data");
            }

            var boundary = contentType.Split("boundary=").LastOrDefault();
            if (string.IsNullOrEmpty(boundary))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Missing boundary in multipart request");
            }

            // Read body stream into byte array
            using var memoryStream = new MemoryStream();
            await req.Body.CopyToAsync(memoryStream);
            var bodyBytes = memoryStream.ToArray();
            var bodyText = System.Text.Encoding.UTF8.GetString(bodyBytes);
            
            // Parse form fields
            string? vendorName = null;
            byte[]? fileBytes = null;
            string? fileName = null;
            string? fileContentType = null;

            var parts = bodyText.Split("--" + boundary);
            foreach (var part in parts)
            {
                if (part.Contains("Content-Disposition: form-data; name=\"vendorLegalName\""))
                {
                    var lines = part.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    vendorName = lines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.Contains("Content"))?.Trim();
                }
                else if (part.Contains("Content-Disposition: form-data; name=\"file\""))
                {
                    // Extract filename
                    var dispositionLine = part.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                        .FirstOrDefault(l => l.Contains("Content-Disposition"));
                    if (dispositionLine != null)
                    {
                        var fileNameMatch = System.Text.RegularExpressions.Regex.Match(dispositionLine, @"filename=""(.+?)""");
                        if (fileNameMatch.Success)
                        {
                            fileName = fileNameMatch.Groups[1].Value;
                        }
                    }

                    // Extract content type
                    var contentTypeLine = part.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                        .FirstOrDefault(l => l.StartsWith("Content-Type:"));
                    if (contentTypeLine != null)
                    {
                        fileContentType = contentTypeLine.Replace("Content-Type:", "").Trim();
                    }

                    // Extract file bytes (after double newline)
                    var headerEndIndex = part.IndexOf("\r\n\r\n");
                    if (headerEndIndex < 0) headerEndIndex = part.IndexOf("\n\n");
                    if (headerEndIndex > 0)
                    {
                        var fileStartIndex = headerEndIndex + (part.Contains("\r\n\r\n") ? 4 : 2);
                        var fileEndIndex = part.LastIndexOf("\r\n");
                        if (fileEndIndex < 0) fileEndIndex = part.LastIndexOf("\n");
                        if (fileEndIndex > fileStartIndex)
                        {
                            var filePartText = part.Substring(fileStartIndex, fileEndIndex - fileStartIndex);
                            // Convert back to bytes from the original body
                            var filePartStartInBody = bodyText.IndexOf(filePartText);
                            if (filePartStartInBody > 0)
                            {
                                fileBytes = bodyBytes.Skip(filePartStartInBody).Take(fileEndIndex - fileStartIndex).ToArray();
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(vendorName))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Vendor name is required");
            }

            if (fileBytes == null || fileBytes.Length == 0)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "File is required");
            }

            _logger.LogInformation("Received file: {FileName}, size: {Size} bytes, vendor: {Vendor}", 
                fileName, fileBytes.Length, vendorName);

            // Extract data using Azure Document Intelligence
            var extractionResult = await _extractionService.ExtractFromBytesAsync(fileBytes, fileContentType ?? "application/pdf");

            // Create invoice with extracted data
            var invoice = new InvoiceEntity
            {
                VendorLegalName = extractionResult.VendorLegalName ?? vendorName,
                InvoiceNumber = extractionResult.InvoiceNumber ?? $"UPLOAD-{DateTime.UtcNow:yyyyMMddHHmmss}",
                InvoiceDate = extractionResult.InvoiceDate ?? DateTime.UtcNow,
                InvoiceCurrency = extractionResult.Currency ?? "INR",
                TotalAmount = extractionResult.TotalAmount,
                TaxAmount = extractionResult.TaxAmount,
                PoNumber = extractionResult.PoNumber ?? "",
                LineItemsSummary = extractionResult.LineItemsSummary ?? "",
                AttachmentName = fileName ?? "uploaded-invoice.pdf",
                Status = InvoiceStatus.Received,
                CreatedBy = "upload-api"
            };

            var created = await _invoiceService.CreateAsync(invoice);

            return await CreateJsonResponse(req, HttpStatusCode.Created, new
            {
                invoice = created,
                extraction = new
                {
                    confidence = extractionResult.Confidence,
                    fieldConfidences = extractionResult.FieldConfidences,
                    extractedData = new
                    {
                        vendorLegalName = extractionResult.VendorLegalName,
                        invoiceNumber = extractionResult.InvoiceNumber,
                        invoiceDate = extractionResult.InvoiceDate,
                        currency = extractionResult.Currency,
                        totalAmount = extractionResult.TotalAmount,
                        taxAmount = extractionResult.TaxAmount,
                        poNumber = extractionResult.PoNumber
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload and extract invoice");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, 
                $"Failed to process invoice: {ex.Message}");
        }
    }

    /// <summary>
    /// POST /api/invoices/{id}/validate — Run validation on an invoice and update its status.
    /// </summary>
    [Function("InvoiceValidate")]
    public async Task<HttpResponseData> ValidateInvoice(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices/{id}/validate")] HttpRequestData req,
        string id)
    {
        try
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, $"Invoice '{id}' not found.");
            }

            var result = await _validationService.ValidateAsync(invoice);

            // Update invoice status based on validation result
            if (result.IsValid)
            {
                invoice.Status = InvoiceStatus.ReadyForZoho;
                invoice.ExceptionReason = string.Empty;
                invoice.ExceptionNotes = string.Empty;
            }
            else
            {
                invoice.Status = InvoiceStatus.Exception;
                invoice.ExceptionReason = string.Join("; ", result.Errors);
            }

            invoice.UpdatedBy = "validation-service";
            await _invoiceService.UpdateAsync(invoice);

            return await CreateJsonResponse(req, HttpStatusCode.OK, new
            {
                invoiceId = id,
                isValid = result.IsValid,
                newStatus = invoice.Status,
                errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate invoice {InvoiceId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to validate invoice.");
        }
    }

    /// <summary>
    /// POST /api/invoices/{id}/extract — Extract invoice data from an attachment using AI.
    /// </summary>
    [Function("InvoiceExtract")]
    public async Task<HttpResponseData> ExtractInvoice(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices/{id}/extract")] HttpRequestData req,
        string id)
    {
        try
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, $"Invoice '{id}' not found.");
            }

            _logger.LogWarning("Using mock extraction - file upload not implemented. Vendor: {Vendor}", invoice.VendorLegalName);
            
            // Mock extraction that at least uses the vendor from the invoice
            var result = await _extractionService.ExtractAsync($"blob://invoice-{id}");
            
            // Override vendor with the one from invoice creation
            if (!string.IsNullOrEmpty(invoice.VendorLegalName))
            {
                result.VendorLegalName = invoice.VendorLegalName;
            }

            // Update invoice with extracted data
            invoice.VendorLegalName = result.VendorLegalName;
            invoice.InvoiceNumber = result.InvoiceNumber;
            if (result.InvoiceDate.HasValue)
            {
                invoice.InvoiceDate = DateTime.SpecifyKind(result.InvoiceDate.Value, DateTimeKind.Utc);
            }
            invoice.InvoiceCurrency = result.Currency;
            invoice.TotalAmount = result.TotalAmount;
            invoice.TaxAmount = result.TaxAmount;
            invoice.PoNumber = result.PoNumber;
            invoice.LineItemsSummary = result.LineItemsSummary;
            invoice.UpdatedBy = "extraction-service";

            await _invoiceService.UpdateAsync(invoice);

            return await CreateJsonResponse(req, HttpStatusCode.OK, new
            {
                invoiceId = id,
                extractedData = new
                {
                    vendorLegalName = result.VendorLegalName,
                    invoiceNumber = result.InvoiceNumber,
                    invoiceDate = result.InvoiceDate,
                    currency = result.Currency,
                    totalAmount = result.TotalAmount,
                    taxAmount = result.TaxAmount,
                    poNumber = result.PoNumber,
                    lineItemsSummary = result.LineItemsSummary
                },
                confidence = result.Confidence,
                fieldConfidences = result.FieldConfidences,
                note = "Mock extraction - file upload not yet implemented"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract invoice {InvoiceId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to extract invoice data.");
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
