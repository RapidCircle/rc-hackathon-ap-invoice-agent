using Azure;
using Azure.Data.Tables;

namespace Api.Models;

/// <summary>
/// Represents an authentication session.
/// Stored in Azure Table Storage with SessionId as the RowKey.
/// </summary>
public class AuthSession : ITableEntity
{
    /// <summary>
    /// Partition key for Table Storage. Always "Session" for this entity type.
    /// </summary>
    public string PartitionKey { get; set; } = "Session";

    /// <summary>
    /// Row key for Table Storage. Uses the SessionId.
    /// </summary>
    public string RowKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure Table Storage timestamp.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Azure Table Storage ETag for optimistic concurrency.
    /// </summary>
    public ETag ETag { get; set; }

    /// <summary>
    /// Unique identifier for the session.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// The user ID this session belongs to.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address (cached for convenience).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Whether MFA has been completed for this session.
    /// </summary>
    public bool MfaCompleted { get; set; }

    /// <summary>
    /// When this session expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When this session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
