using Api.Models;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Logging;

namespace Api.Services;

/// <summary>
/// Extracts invoice data from attachments using Azure AI Document Intelligence.
/// </summary>
public class ExtractionService
{
    private readonly ILogger<ExtractionService> _logger;
    private readonly DocumentAnalysisClient _documentClient;

    public ExtractionService(ILogger<ExtractionService> logger)
    {
        _logger = logger;

        var endpoint = Environment.GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT")
            ?? throw new InvalidOperationException("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT is not configured.");
        var key = Environment.GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_KEY")
            ?? throw new InvalidOperationException("AZURE_DOCUMENT_INTELLIGENCE_KEY is not configured.");
        _documentClient = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));
    }

    /// <summary>
    /// Extracts invoice fields from an attachment URL or file bytes.
    /// </summary>
    /// <param name="attachmentUrl">URL or identifier of the invoice attachment.</param>
    /// <returns>Extraction result with field values and confidence scores.</returns>
    public async Task<ExtractionResult> ExtractAsync(string attachmentUrl)
    {
        _logger.LogInformation("Extracting invoice data from attachment: {AttachmentUrl}", attachmentUrl);

        // For now, since we don't have blob storage URLs yet, return mock data
        // In production, this would analyze the actual document from blob storage
        _logger.LogWarning("Using mock extraction - blob storage integration pending");
        
        await Task.Delay(500);
        
        var result = new ExtractionResult
        {
            VendorLegalName = "Infosys Ltd",
            InvoiceNumber = $"INF-2026-03-{Random.Shared.Next(7000, 8000)}",
            InvoiceDate = DateTime.SpecifyKind(new DateTime(2026, 3, 28), DateTimeKind.Utc),
            Currency = "INR",
            TotalAmount = 1875000.00,
            TaxAmount = 286016.95,
            PoNumber = "PO-2026-102",
            LineItemsSummary = "[{\"description\":\"Cloud Migration Services - Azure\",\"amount\":1250000},{\"description\":\"DevOps Setup & Training\",\"amount\":625000}]",
            Confidence = 0.94,
            FieldConfidences = new Dictionary<string, double>
            {
                { "VendorLegalName", 0.96 },
                { "InvoiceNumber", 0.98 },
                { "InvoiceDate", 0.95 },
                { "Currency", 0.99 },
                { "TotalAmount", 0.97 },
                { "TaxAmount", 0.93 },
                { "PoNumber", 0.91 },
                { "LineItems", 0.88 }
            }
        };

        _logger.LogInformation(
            "Extraction complete: vendor={Vendor}, invoice={InvoiceNumber}, amount={Amount}",
            result.VendorLegalName, result.InvoiceNumber, result.TotalAmount);

        return result;
    }
    
    /// <summary>
    /// Extracts invoice data from raw file bytes (for real-time upload processing).
    /// </summary>
    public async Task<ExtractionResult> ExtractFromBytesAsync(byte[] fileBytes, string contentType)
    {
        _logger.LogInformation("Extracting invoice from {Size} bytes, type: {ContentType}", 
            fileBytes.Length, contentType);

        using var stream = new MemoryStream(fileBytes);
        var operation = await _documentClient.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-invoice",
            stream);

        var analyzeResult = operation.Value;
        var document = analyzeResult.Documents.FirstOrDefault();

        if (document == null)
        {
            _logger.LogWarning("No invoice document found in extraction result");
            return new ExtractionResult { Confidence = 0 };
        }

        var result = new ExtractionResult
        {
            Confidence = (double)document.Confidence,
            FieldConfidences = new Dictionary<string, double>()
        };

        var fields = document.Fields;

        if (fields.TryGetValue("VendorName", out var vendorField) && vendorField.FieldType == DocumentFieldType.String)
        {
            result.VendorLegalName = vendorField.Value.AsString();
            result.FieldConfidences["VendorLegalName"] = (double)(vendorField.Confidence ?? 0);
        }

        if (fields.TryGetValue("InvoiceId", out var invoiceIdField) && invoiceIdField.FieldType == DocumentFieldType.String)
        {
            result.InvoiceNumber = invoiceIdField.Value.AsString();
            result.FieldConfidences["InvoiceNumber"] = (double)(invoiceIdField.Confidence ?? 0);
        }

        if (fields.TryGetValue("InvoiceDate", out var dateField) && dateField.FieldType == DocumentFieldType.Date)
        {
            result.InvoiceDate = dateField.Value.AsDate().DateTime;
            result.FieldConfidences["InvoiceDate"] = (double)(dateField.Confidence ?? 0);
        }

        if (fields.TryGetValue("InvoiceTotal", out var totalField) && totalField.FieldType == DocumentFieldType.Currency)
        {
            var currency = totalField.Value.AsCurrency();
            result.TotalAmount = (double)currency.Amount;
            result.Currency = "INR"; // Default to INR for now
            result.FieldConfidences["TotalAmount"] = (double)(totalField.Confidence ?? 0);
        }

        if (fields.TryGetValue("TotalTax", out var taxField) && taxField.FieldType == DocumentFieldType.Currency)
        {
            result.TaxAmount = (double)taxField.Value.AsCurrency().Amount;
            result.FieldConfidences["TaxAmount"] = (double)(taxField.Confidence ?? 0);
        }

        if (fields.TryGetValue("PurchaseOrder", out var poField) && poField.FieldType == DocumentFieldType.String)
        {
            result.PoNumber = poField.Value.AsString();
            result.FieldConfidences["PoNumber"] = (double)(poField.Confidence ?? 0);
        }

        // Extract line items
        if (fields.TryGetValue("Items", out var itemsField) && itemsField.FieldType == DocumentFieldType.List)
        {
            var items = new List<object>();
            foreach (var item in itemsField.Value.AsList())
            {
                if (item.FieldType == DocumentFieldType.Dictionary)
                {
                    var itemDict = item.Value.AsDictionary();
                    var lineItem = new Dictionary<string, object>();
                    
                    if (itemDict.TryGetValue("Description", out var desc))
                        lineItem["description"] = desc.Content;
                    if (itemDict.TryGetValue("Amount", out var amt) && amt.FieldType == DocumentFieldType.Currency)
                        lineItem["amount"] = (double)amt.Value.AsCurrency().Amount;
                    
                    items.Add(lineItem);
                }
            }
            result.LineItemsSummary = System.Text.Json.JsonSerializer.Serialize(items);
            result.FieldConfidences["LineItems"] = (double)(itemsField.Confidence ?? 0);
        }

        _logger.LogInformation(
            "Real extraction complete: vendor={Vendor}, invoice={InvoiceNumber}, amount={Amount}, confidence={Confidence}",
            result.VendorLegalName, result.InvoiceNumber, result.TotalAmount, result.Confidence);

        return result;
    }
}
