using Azure;
using Azure.Data.Tables;

namespace Api.Models;

/// <summary>
/// Represents a user in the system.
/// Stored in Azure Table Storage with Email as the RowKey.
/// </summary>
public class User : ITableEntity
{
    /// <summary>
    /// Partition key for Table Storage. Always "User" for this entity type.
    /// </summary>
    public string PartitionKey { get; set; } = "User";

    /// <summary>
    /// Row key for Table Storage. Uses the UserId.
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
    /// Unique identifier for the user.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address. Used as the login identifier.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full display name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user has system administrator privileges.
    /// </summary>
    public bool IsSystemAdmin { get; set; }

    /// <summary>
    /// User status: Active, Suspended, or Inactive.
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// User's preferred notification method.
    /// </summary>
    public string NotificationPreference { get; set; } = "Email";

    /// <summary>
    /// Brief description of this user's persona (for demo purposes).
    /// </summary>
    public string PersonaDescription { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of roles for this user.
    /// </summary>
    public string Roles { get; set; } = "authenticated";
}
