using Azure;
using Azure.Data.Tables;

namespace Api.Models;

/// <summary>
/// Represents a vendor in the vendor master.
/// Stored in Azure Table Storage with PartitionKey="Vendor" and RowKey=GUID.
/// </summary>
public class VendorEntity : ITableEntity
{
    /// <summary>
    /// Partition key for Table Storage. Always "Vendor" for this entity type.
    /// </summary>
    public string PartitionKey { get; set; } = "Vendor";

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
    /// Official legal name of the vendor.
    /// </summary>
    public string LegalName { get; set; } = string.Empty;

    /// <summary>
    /// Trading or brand name of the vendor.
    /// </summary>
    public string TradingName { get; set; } = string.Empty;

    /// <summary>
    /// Tax identification number (GSTIN, PAN, etc.).
    /// </summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the vendor is currently active in the system.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this record was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this record was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
