using Azure.Data.Tables;

namespace Api.Services;

/// <summary>
/// Provides typed accessors for Azure Table Storage tables used by the AP Invoice Agent.
/// Uses the STORAGE connection string (not AzureWebJobsStorage, which is reserved by SWA).
/// </summary>
public class TableStorageContext
{
    private readonly TableServiceClient _serviceClient;

    /// <summary>
    /// Table name for invoice records.
    /// </summary>
    public const string InvoicesTable = "Invoices";

    /// <summary>
    /// Table name for vendor master data.
    /// </summary>
    public const string VendorsTable = "Vendors";

    /// <summary>
    /// Table name for batch records.
    /// </summary>
    public const string BatchesTable = "Batches";

    public TableStorageContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("STORAGE")
            ?? throw new InvalidOperationException("STORAGE connection string is not configured.");
        _serviceClient = new TableServiceClient(connectionString);
    }

    /// <summary>
    /// Gets a TableClient for the Invoices table.
    /// </summary>
    public TableClient Invoices => _serviceClient.GetTableClient(InvoicesTable);

    /// <summary>
    /// Gets a TableClient for the Vendors table.
    /// </summary>
    public TableClient Vendors => _serviceClient.GetTableClient(VendorsTable);

    /// <summary>
    /// Gets a TableClient for the Batches table.
    /// </summary>
    public TableClient Batches => _serviceClient.GetTableClient(BatchesTable);

    /// <summary>
    /// Ensures all required tables exist in storage.
    /// Call during application startup or before first use.
    /// </summary>
    public async Task EnsureTablesExistAsync()
    {
        await Invoices.CreateIfNotExistsAsync();
        await Vendors.CreateIfNotExistsAsync();
        await Batches.CreateIfNotExistsAsync();
    }
}
