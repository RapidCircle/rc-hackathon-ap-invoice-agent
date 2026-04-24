namespace Api.Models;

/// <summary>
/// String constants for invoice processing statuses.
/// Using static string constants instead of enums for Table Storage compatibility.
/// </summary>
public static class InvoiceStatus
{
    public const string Received = "Received";
    public const string ReadyForZoho = "ReadyForZoho";
    public const string Exception = "Exception";
    public const string InReview = "InReview";
    public const string Corrected = "Corrected";

    /// <summary>
    /// All valid status values.
    /// </summary>
    public static readonly string[] All = { Received, ReadyForZoho, Exception, InReview, Corrected };

    /// <summary>
    /// Checks if a status string is valid.
    /// </summary>
    public static bool IsValid(string status) => All.Contains(status);
}

/// <summary>
/// String constants for batch processing statuses.
/// </summary>
public static class BatchStatus
{
    public const string Pending = "Pending";
    public const string Pushed = "Pushed";
    public const string Failed = "Failed";

    public static readonly string[] All = { Pending, Pushed, Failed };

    public static bool IsValid(string status) => All.Contains(status);
}
