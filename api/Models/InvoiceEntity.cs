using Azure;
using Azure.Data.Tables;

namespace Api.Models;

/// <summary>
/// Represents an AP invoice in the system.
/// Stored in Azure Table Storage with PartitionKey="Invoice" and RowKey=GUID.
/// </summary>
public class InvoiceEntity : ITableEntity
{
    /// <summary>
    /// Partition key for Table Storage. Always "Invoice" for this entity type.
    /// </summary>
    public string PartitionKey { get; set; } = "Invoice";

    /// <summary>
    /// Row key for Table Storage. Uses a GUID as unique identifier.
    /// </summary>
    public string RowKey { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Azure Table Storage timestamp.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Azure Table Storage ETag for optimistic concurrency.
    /// </summary>
    public ETag ETag { get; set; }

    /// <summary>
    /// Legal name of the vendor on the invoice. Must match vendor master.
    /// </summary>
    public string VendorLegalName { get; set; } = string.Empty;

    /// <summary>
    /// Invoice number from the vendor document.
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Date of the invoice (UTC).
    /// </summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>
    /// Currency code (ISO 4217) for the invoice, e.g. INR, USD, EUR.
    /// </summary>
    public string InvoiceCurrency { get; set; } = "INR";

    /// <summary>
    /// Total amount of the invoice including tax.
    /// </summary>
    public double TotalAmount { get; set; }

    /// <summary>
    /// Tax amount on the invoice.
    /// </summary>
    public double TaxAmount { get; set; }

    /// <summary>
    /// Purchase order number linked to this invoice.
    /// </summary>
    public string PoNumber { get; set; } = string.Empty;

    /// <summary>
    /// Summary of line items as a JSON string.
    /// </summary>
    public string LineItemsSummary { get; set; } = string.Empty;

    /// <summary>
    /// Current processing status of the invoice.
    /// Values: Received, ReadyForZoho, Exception, InReview, Corrected.
    /// </summary>
    public string Status { get; set; } = "Received";

    /// <summary>
    /// Reason for exception status, if applicable.
    /// </summary>
    public string ExceptionReason { get; set; } = string.Empty;

    /// <summary>
    /// Notes added during exception review.
    /// </summary>
    public string ExceptionNotes { get; set; } = string.Empty;

    /// <summary>
    /// Email address that sent this invoice.
    /// </summary>
    public string EmailSender { get; set; } = string.Empty;

    /// <summary>
    /// Subject line of the email that contained this invoice.
    /// </summary>
    public string EmailSubject { get; set; } = string.Empty;

    /// <summary>
    /// Date the invoice email was received (UTC).
    /// </summary>
    public DateTime EmailReceivedDate { get; set; }

    /// <summary>
    /// Name of the attachment file containing the invoice.
    /// </summary>
    public string AttachmentName { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score from AI extraction (0.0 to 1.0).
    /// </summary>
    public double ExtractionConfidence { get; set; }

    /// <summary>
    /// When this record was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this record was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User who created this record.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// User who last updated this record.
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// JSON string containing linked document references.
    /// </summary>
    public string LinkedDocuments { get; set; } = string.Empty;
}
