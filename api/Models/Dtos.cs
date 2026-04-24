namespace Api.Models;

/// <summary>
/// Request DTO for creating a new invoice.
/// </summary>
public class InvoiceCreateRequest
{
    public string VendorLegalName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public string Currency { get; set; } = "INR";
    public double TotalAmount { get; set; }
    public double TaxAmount { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public string LineItemsSummary { get; set; } = string.Empty;
    public string EmailSender { get; set; } = string.Empty;
    public string EmailSubject { get; set; } = string.Empty;
    public string AttachmentName { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating an existing invoice. All fields are nullable for partial update.
/// </summary>
public class InvoiceUpdateRequest
{
    public string? VendorLegalName { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Currency { get; set; }
    public double? TotalAmount { get; set; }
    public double? TaxAmount { get; set; }
    public string? PoNumber { get; set; }
    public string? LineItemsSummary { get; set; }
    public string? Status { get; set; }
    public string? ExceptionReason { get; set; }
    public string? ExceptionNotes { get; set; }
}

/// <summary>
/// Request DTO for creating a new vendor.
/// </summary>
public class VendorCreateRequest
{
    public string LegalName { get; set; } = string.Empty;
    public string TradingName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating a vendor. All fields are nullable for partial update.
/// </summary>
public class VendorUpdateRequest
{
    public string? LegalName { get; set; }
    public string? TradingName { get; set; }
    public string? TaxId { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request DTO for creating a new batch.
/// </summary>
public class BatchCreateRequest
{
    public DateTime CutoffDateTime { get; set; }
}

/// <summary>
/// Dashboard statistics for invoices.
/// </summary>
public class InvoiceStatsDto
{
    public int ReceivedCount { get; set; }
    public int ReadyForZohoCount { get; set; }
    public int ExceptionCount { get; set; }
    public int InReviewCount { get; set; }
    public int CorrectedCount { get; set; }
    public int TotalCount { get; set; }
    public double TotalAmount { get; set; }
}

/// <summary>
/// Lightweight invoice item for list views.
/// </summary>
public class InvoiceListItem
{
    public string Id { get; set; } = string.Empty;
    public string VendorLegalName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string InvoiceCurrency { get; set; } = string.Empty;
    public double TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ExceptionReason { get; set; } = string.Empty;
    public string PoNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Result of invoice validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Result of AI document extraction.
/// </summary>
public class ExtractionResult
{
    public string VendorLegalName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public string Currency { get; set; } = string.Empty;
    public double TotalAmount { get; set; }
    public double TaxAmount { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public string LineItemsSummary { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public Dictionary<string, double> FieldConfidences { get; set; } = new();
}
