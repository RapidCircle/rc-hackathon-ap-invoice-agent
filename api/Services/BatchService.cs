using System.Text.Json;
using Api.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Api.Services;

/// <summary>
/// Service for batch operations — grouping validated invoices and pushing to Zoho.
/// </summary>
public class BatchService
{
    private readonly TableStorageContext _storage;
    private readonly InvoiceService _invoiceService;
    private readonly ILogger<BatchService> _logger;

    public BatchService(TableStorageContext storage, InvoiceService invoiceService, ILogger<BatchService> logger)
    {
        _storage = storage;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new batch from all ReadyForZoho invoices created before the cutoff date.
    /// </summary>
    public async Task<BatchEntity> CreateBatchAsync(DateTime cutoffDateTime)
    {
        await _storage.EnsureTablesExistAsync();

        // Find all ReadyForZoho invoices before cutoff
        var allReady = await _invoiceService.ListAsync(InvoiceStatus.ReadyForZoho);
        var eligibleInvoices = allReady
            .Where(i => i.CreatedAt <= DateTime.SpecifyKind(cutoffDateTime, DateTimeKind.Utc))
            .ToList();

        var invoiceIds = eligibleInvoices.Select(i => i.RowKey).ToList();

        var batch = new BatchEntity
        {
            PartitionKey = "Batch",
            RowKey = Guid.NewGuid().ToString(),
            BatchDate = DateTime.UtcNow,
            CutoffDateTime = DateTime.SpecifyKind(cutoffDateTime, DateTimeKind.Utc),
            Status = BatchStatus.Pending,
            InvoiceCount = eligibleInvoices.Count,
            TotalAmount = eligibleInvoices.Sum(i => i.TotalAmount),
            InvoiceIds = JsonSerializer.Serialize(invoiceIds)
        };

        await _storage.Batches.AddEntityAsync(batch);
        _logger.LogInformation("Created batch {BatchId} with {Count} invoices, total {Amount:N2}",
            batch.RowKey, batch.InvoiceCount, batch.TotalAmount);

        return batch;
    }

    /// <summary>
    /// Pushes a batch to Zoho (mock implementation for hackathon).
    /// Updates all included invoices and the batch status.
    /// </summary>
    public async Task<BatchEntity?> PushBatchAsync(string batchId, string pushedBy = "system")
    {
        var batch = await GetByIdAsync(batchId);
        if (batch == null) return null;

        if (batch.Status == BatchStatus.Pushed)
        {
            _logger.LogWarning("Batch {BatchId} has already been pushed", batchId);
            return batch;
        }

        try
        {
            // Parse invoice IDs from the batch
            var invoiceIds = JsonSerializer.Deserialize<List<string>>(batch.InvoiceIds) ?? new List<string>();

            _logger.LogInformation("Pushing batch {BatchId} with {Count} invoices to Zoho (mock)...", batchId, invoiceIds.Count);

            // Mock: simulate Zoho API call
            await Task.Delay(300);

            // Update each invoice status (in a real system, only on successful Zoho push)
            foreach (var invoiceId in invoiceIds)
            {
                var invoice = await _invoiceService.GetByIdAsync(invoiceId);
                if (invoice != null)
                {
                    invoice.Status = "Pushed";
                    invoice.UpdatedBy = pushedBy;
                    await _invoiceService.UpdateAsync(invoice);
                }
            }

            // Update batch status
            batch.Status = BatchStatus.Pushed;
            batch.PushedAt = DateTime.UtcNow;
            batch.PushedBy = pushedBy;
            await _storage.Batches.UpsertEntityAsync(batch, TableUpdateMode.Replace);

            _logger.LogInformation("Batch {BatchId} pushed successfully to Zoho (mock)", batchId);
            return batch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push batch {BatchId}", batchId);

            batch.Status = BatchStatus.Failed;
            await _storage.Batches.UpsertEntityAsync(batch, TableUpdateMode.Replace);

            return batch;
        }
    }

    /// <summary>
    /// Retrieves a batch by its ID (RowKey).
    /// </summary>
    public async Task<BatchEntity?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _storage.Batches.GetEntityAsync<BatchEntity>("Batch", id);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <summary>
    /// Lists all batches, ordered by batch date descending.
    /// </summary>
    public async Task<List<BatchEntity>> ListBatchesAsync()
    {
        var batches = new List<BatchEntity>();

        await foreach (var entity in _storage.Batches
            .QueryAsync<BatchEntity>(e => e.PartitionKey == "Batch"))
        {
            batches.Add(entity);
        }

        batches.Sort((a, b) => b.BatchDate.CompareTo(a.BatchDate));

        _logger.LogDebug("Listed {Count} batches", batches.Count);
        return batches;
    }
}
