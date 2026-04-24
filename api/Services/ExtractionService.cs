using Api.Models;
using Microsoft.Extensions.Logging;

namespace Api.Services;

/// <summary>
/// Extracts invoice data from attachments using Azure AI Document Intelligence.
/// For the hackathon, this uses a mock implementation with realistic results.
/// The real implementation using Azure.AI.FormRecognizer is included but commented out.
/// </summary>
public class ExtractionService
{
    private readonly ILogger<ExtractionService> _logger;

    // Uncomment for real Azure AI Document Intelligence integration:
    // private readonly DocumentAnalysisClient _documentClient;

    public ExtractionService(ILogger<ExtractionService> logger)
    {
        _logger = logger;

        // Uncomment for real Azure AI Document Intelligence integration:
        // var endpoint = Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_ENDPOINT")
        //     ?? throw new InvalidOperationException("DOCUMENT_INTELLIGENCE_ENDPOINT is not configured.");
        // var key = Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_KEY")
        //     ?? throw new InvalidOperationException("DOCUMENT_INTELLIGENCE_KEY is not configured.");
        // _documentClient = new DocumentAnalysisClient(new Uri(endpoint), new Azure.AzureKeyCredential(key));
    }

    /// <summary>
    /// Extracts invoice fields from an attachment URL.
    /// Hackathon implementation: returns mock extraction results with realistic confidence scores.
    /// </summary>
    /// <param name="attachmentUrl">URL of the invoice attachment to process.</param>
    /// <returns>Extraction result with field values and confidence scores.</returns>
    public async Task<ExtractionResult> ExtractAsync(string attachmentUrl)
    {
        _logger.LogInformation("Extracting invoice data from attachment: {AttachmentUrl}", attachmentUrl);

        // --- Mock implementation for hackathon demo ---
        // Simulates realistic extraction results with varying confidence levels
        await Task.Delay(500); // Simulate processing time

        var result = new ExtractionResult
        {
            VendorLegalName = "Infosys Ltd",
            InvoiceNumber = $"INF-{DateTime.UtcNow:yyyyMM}-{Random.Shared.Next(1000, 9999)}",
            InvoiceDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)), DateTimeKind.Utc),
            Currency = "INR",
            TotalAmount = Math.Round(Random.Shared.NextDouble() * 2000000 + 100000, 2),
            TaxAmount = 0,
            PoNumber = $"PO-2026-{Random.Shared.Next(100, 999)}",
            LineItemsSummary = "[{\"description\":\"IT Consulting Services - Q1 2026\",\"quantity\":1,\"unitPrice\":850000,\"amount\":850000},{\"description\":\"Cloud Infrastructure Management\",\"quantity\":1,\"unitPrice\":350000,\"amount\":350000}]",
            Confidence = 0.92,
            FieldConfidences = new Dictionary<string, double>
            {
                { "VendorLegalName", 0.95 },
                { "InvoiceNumber", 0.98 },
                { "InvoiceDate", 0.91 },
                { "Currency", 0.99 },
                { "TotalAmount", 0.94 },
                { "TaxAmount", 0.88 },
                { "PoNumber", 0.85 },
                { "LineItems", 0.82 }
            }
        };

        // Calculate tax as 18% GST
        result.TaxAmount = Math.Round(result.TotalAmount * 0.18, 2);

        _logger.LogInformation(
            "Extraction complete: vendor={Vendor}, invoice={InvoiceNumber}, amount={Amount}, confidence={Confidence}",
            result.VendorLegalName, result.InvoiceNumber, result.TotalAmount, result.Confidence);

        return result;

        // --- Real implementation using Azure AI Document Intelligence ---
        // Uncomment the below and remove the mock above for production use:
        //
        // var operation = await _documentClient.AnalyzeDocumentFromUriAsync(
        //     WaitUntil.Completed,
        //     "prebuilt-invoice",
        //     new Uri(attachmentUrl));
        //
        // var analyzeResult = operation.Value;
        // var document = analyzeResult.Documents.FirstOrDefault();
        //
        // if (document == null)
        // {
        //     _logger.LogWarning("No invoice document found in extraction result");
        //     return new ExtractionResult { Confidence = 0 };
        // }
        //
        // var result = new ExtractionResult
        // {
        //     Confidence = (double)document.Confidence,
        //     FieldConfidences = new Dictionary<string, double>()
        // };
        //
        // if (document.Fields.TryGetValue("VendorName", out var vendorField))
        // {
        //     result.VendorLegalName = vendorField.Value.AsString();
        //     result.FieldConfidences["VendorLegalName"] = (double)(vendorField.Confidence ?? 0);
        // }
        //
        // if (document.Fields.TryGetValue("InvoiceId", out var invoiceIdField))
        // {
        //     result.InvoiceNumber = invoiceIdField.Value.AsString();
        //     result.FieldConfidences["InvoiceNumber"] = (double)(invoiceIdField.Confidence ?? 0);
        // }
        //
        // if (document.Fields.TryGetValue("InvoiceDate", out var dateField))
        // {
        //     result.InvoiceDate = dateField.Value.AsDate();
        //     result.FieldConfidences["InvoiceDate"] = (double)(dateField.Confidence ?? 0);
        // }
        //
        // if (document.Fields.TryGetValue("InvoiceTotal", out var totalField))
        // {
        //     result.TotalAmount = (double)totalField.Value.AsCurrency().Amount;
        //     result.Currency = totalField.Value.AsCurrency().Code ?? "INR";
        //     result.FieldConfidences["TotalAmount"] = (double)(totalField.Confidence ?? 0);
        // }
        //
        // if (document.Fields.TryGetValue("TotalTax", out var taxField))
        // {
        //     result.TaxAmount = (double)taxField.Value.AsCurrency().Amount;
        //     result.FieldConfidences["TaxAmount"] = (double)(taxField.Confidence ?? 0);
        // }
        //
        // if (document.Fields.TryGetValue("PurchaseOrder", out var poField))
        // {
        //     result.PoNumber = poField.Value.AsString();
        //     result.FieldConfidences["PoNumber"] = (double)(poField.Confidence ?? 0);
        // }
        //
        // return result;
    }
}
