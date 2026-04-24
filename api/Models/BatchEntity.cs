using Azure;
using Azure.Data.Tables;

namespace Api.Models;

/// <summary>
/// Represents a batch of invoices ready to be pushed to Zoho.
/// Stored in Azure Table Storage with PartitionKey="Batch" and RowKey=GUID.
/// </summary>
public class BatchEntity : ITableEntity
{
    /// <summary>
    /// Partition key for Table Storage. Always "Batch" for this entity type.
    /// </summary>
    public string PartitionKey { get; set; } = "Batch";

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
    /// Date this batch was created (UTC).
    /// </summary>
    public DateTime BatchDate { get; set; }

    /// <summary>
    /// Cutoff date/time — invoices received before this time are included (UTC).
    /// </summary>
    public DateTime CutoffDateTime { get; set; }

    /// <summary>
    /// Current batch status: Pending, Pushed, Failed.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Number of invoices in this batch.
    /// </summary>
    public int InvoiceCount { get; set; }

    /// <summary>
    /// Total monetary amount of all invoices in this batch.
    /// </summary>
    public double TotalAmount { get; set; }

    /// <summary>
    /// When the batch was pushed to Zoho (UTC). Null if not yet pushed.
    /// </summary>
    public DateTime? PushedAt { get; set; }

    /// <summary>
    /// User who pushed this batch.
    /// </summary>
    public string PushedBy { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of invoice RowKey IDs included in this batch.
    /// </summary>
    public string InvoiceIds { get; set; } = "[]";
}
