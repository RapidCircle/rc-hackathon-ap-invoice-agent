using Api.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Api.Services;

/// <summary>
/// Service for CRUD operations on invoice entities in Azure Table Storage.
/// </summary>
public class InvoiceService
{
    private readonly TableStorageContext _storage;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(TableStorageContext storage, ILogger<InvoiceService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new invoice entity.
    /// </summary>
    public async Task<InvoiceEntity> CreateAsync(InvoiceEntity invoice)
    {
        await _storage.EnsureTablesExistAsync();

        invoice.PartitionKey = "Invoice";
        if (string.IsNullOrEmpty(invoice.RowKey))
            invoice.RowKey = Guid.NewGuid().ToString();

        invoice.CreatedAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _storage.Invoices.AddEntityAsync(invoice);
        _logger.LogInformation("Created invoice {InvoiceId} for vendor {Vendor}", invoice.RowKey, invoice.VendorLegalName);

        return invoice;
    }

    /// <summary>
    /// Retrieves an invoice by its ID (RowKey).
    /// Returns null if not found.
    /// </summary>
    public async Task<InvoiceEntity?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _storage.Invoices.GetEntityAsync<InvoiceEntity>("Invoice", id);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <summary>
    /// Updates an existing invoice entity.
    /// </summary>
    public async Task<InvoiceEntity> UpdateAsync(InvoiceEntity invoice)
    {
        invoice.UpdatedAt = DateTime.UtcNow;
        await _storage.Invoices.UpsertEntityAsync(invoice, TableUpdateMode.Replace);
        _logger.LogInformation("Updated invoice {InvoiceId}", invoice.RowKey);
        return invoice;
    }

    /// <summary>
    /// Lists all invoices, optionally filtered by status.
    /// </summary>
    public async Task<List<InvoiceEntity>> ListAsync(string? statusFilter = null)
    {
        var invoices = new List<InvoiceEntity>();

        await foreach (var entity in _storage.Invoices
            .QueryAsync<InvoiceEntity>(e => e.PartitionKey == "Invoice"))
        {
            if (statusFilter == null || entity.Status == statusFilter)
            {
                invoices.Add(entity);
            }
        }

        // Sort by CreatedAt descending (newest first)
        invoices.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

        _logger.LogDebug("Listed {Count} invoices (filter: {Filter})", invoices.Count, statusFilter ?? "none");
        return invoices;
    }

    /// <summary>
    /// Gets dashboard statistics across all invoices.
    /// </summary>
    public async Task<InvoiceStatsDto> GetStatsAsync()
    {
        var all = await ListAsync();

        return new InvoiceStatsDto
        {
            ReceivedCount = all.Count(i => i.Status == InvoiceStatus.Received),
            ReadyForZohoCount = all.Count(i => i.Status == InvoiceStatus.ReadyForZoho),
            ExceptionCount = all.Count(i => i.Status == InvoiceStatus.Exception),
            InReviewCount = all.Count(i => i.Status == InvoiceStatus.InReview),
            CorrectedCount = all.Count(i => i.Status == InvoiceStatus.Corrected),
            TotalCount = all.Count,
            TotalAmount = all.Sum(i => i.TotalAmount)
        };
    }
}
