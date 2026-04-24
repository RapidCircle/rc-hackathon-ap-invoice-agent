using Api.Models;
using Microsoft.Extensions.Logging;

namespace Api.Services;

/// <summary>
/// Validates invoices against mandatory fields and vendor master data.
/// Checks 8 mandatory fields and performs duplicate detection.
/// </summary>
public class ValidationService
{
    private readonly VendorService _vendorService;
    private readonly InvoiceService _invoiceService;
    private readonly ILogger<ValidationService> _logger;

    /// <summary>
    /// Valid ISO 4217 currency codes accepted by the system.
    /// </summary>
    private static readonly HashSet<string> ValidCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "INR", "USD", "EUR", "GBP", "AED", "SGD", "AUD", "CAD", "JPY", "CHF"
    };

    public ValidationService(VendorService vendorService, InvoiceService invoiceService, ILogger<ValidationService> logger)
    {
        _vendorService = vendorService;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Validates an invoice against 8 mandatory fields and vendor master.
    /// Returns a ValidationResult with isValid flag and list of errors.
    /// </summary>
    public async Task<ValidationResult> ValidateAsync(InvoiceEntity invoice)
    {
        var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

        _logger.LogInformation("Validating invoice {InvoiceId} ({InvoiceNumber})", invoice.RowKey, invoice.InvoiceNumber);

        // 1. Vendor Legal Name — must be present
        if (string.IsNullOrWhiteSpace(invoice.VendorLegalName))
        {
            result.Errors.Add("Vendor legal name is required.");
        }
        else
        {
            // Vendor Legal Name — must match an active vendor in the master
            var vendor = await _vendorService.FindByLegalNameAsync(invoice.VendorLegalName);
            if (vendor == null)
            {
                result.Errors.Add($"Vendor '{invoice.VendorLegalName}' not found in active vendor master.");
            }
        }

        // 2. Invoice Number — must be present
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            result.Errors.Add("Invoice number is required.");
        }

        // 3. Invoice Date — must be a valid date (not default)
        if (invoice.InvoiceDate == default)
        {
            result.Errors.Add("Invoice date is required and must be a valid date.");
        }

        // 4. Currency — must be a valid ISO 4217 code
        if (string.IsNullOrWhiteSpace(invoice.InvoiceCurrency))
        {
            result.Errors.Add("Invoice currency is required.");
        }
        else if (!ValidCurrencies.Contains(invoice.InvoiceCurrency))
        {
            result.Errors.Add($"Currency '{invoice.InvoiceCurrency}' is not a recognized ISO 4217 code.");
        }

        // 5. Total Amount — must be greater than zero
        if (invoice.TotalAmount <= 0)
        {
            result.Errors.Add("Total amount must be greater than zero.");
        }

        // 6. Tax Amount — must be zero or positive
        if (invoice.TaxAmount < 0)
        {
            result.Errors.Add("Tax amount cannot be negative.");
        }

        // 7. PO Number — must be present
        if (string.IsNullOrWhiteSpace(invoice.PoNumber))
        {
            result.Errors.Add("Purchase order number is required.");
        }

        // 8. Duplicate check — same vendor + invoice number must be unique
        if (!string.IsNullOrWhiteSpace(invoice.VendorLegalName) && !string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            var existingInvoices = await _invoiceService.ListAsync();
            var duplicate = existingInvoices.FirstOrDefault(i =>
                i.RowKey != invoice.RowKey &&
                i.VendorLegalName.Equals(invoice.VendorLegalName, StringComparison.OrdinalIgnoreCase) &&
                i.InvoiceNumber.Equals(invoice.InvoiceNumber, StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
            {
                result.Errors.Add($"Duplicate invoice: vendor '{invoice.VendorLegalName}' already has invoice '{invoice.InvoiceNumber}'.");
            }
        }

        result.IsValid = result.Errors.Count == 0;

        _logger.LogInformation("Validation result for invoice {InvoiceId}: {IsValid} ({ErrorCount} errors)",
            invoice.RowKey, result.IsValid, result.Errors.Count);

        return result;
    }
}
