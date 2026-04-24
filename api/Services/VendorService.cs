using Api.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Api.Services;

/// <summary>
/// Service for CRUD operations on vendor entities in Azure Table Storage.
/// </summary>
public class VendorService
{
    private readonly TableStorageContext _storage;
    private readonly ILogger<VendorService> _logger;

    public VendorService(TableStorageContext storage, ILogger<VendorService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new vendor entity.
    /// </summary>
    public async Task<VendorEntity> CreateAsync(VendorEntity vendor)
    {
        await _storage.EnsureTablesExistAsync();

        vendor.PartitionKey = "Vendor";
        if (string.IsNullOrEmpty(vendor.RowKey))
            vendor.RowKey = Guid.NewGuid().ToString();

        vendor.CreatedAt = DateTime.UtcNow;
        vendor.UpdatedAt = DateTime.UtcNow;

        await _storage.Vendors.AddEntityAsync(vendor);
        _logger.LogInformation("Created vendor {VendorId}: {LegalName}", vendor.RowKey, vendor.LegalName);

        return vendor;
    }

    /// <summary>
    /// Retrieves a vendor by its ID (RowKey).
    /// Returns null if not found.
    /// </summary>
    public async Task<VendorEntity?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _storage.Vendors.GetEntityAsync<VendorEntity>("Vendor", id);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <summary>
    /// Updates an existing vendor entity.
    /// </summary>
    public async Task<VendorEntity> UpdateAsync(VendorEntity vendor)
    {
        vendor.UpdatedAt = DateTime.UtcNow;
        await _storage.Vendors.UpsertEntityAsync(vendor, TableUpdateMode.Replace);
        _logger.LogInformation("Updated vendor {VendorId}: {LegalName}", vendor.RowKey, vendor.LegalName);
        return vendor;
    }

    /// <summary>
    /// Soft-deletes a vendor by setting IsActive to false.
    /// </summary>
    public async Task<VendorEntity?> DeleteAsync(string id)
    {
        var vendor = await GetByIdAsync(id);
        if (vendor == null) return null;

        vendor.IsActive = false;
        vendor.UpdatedAt = DateTime.UtcNow;
        await _storage.Vendors.UpsertEntityAsync(vendor, TableUpdateMode.Replace);
        _logger.LogInformation("Soft-deleted vendor {VendorId}: {LegalName}", vendor.RowKey, vendor.LegalName);

        return vendor;
    }

    /// <summary>
    /// Lists all vendors. By default only returns active vendors.
    /// </summary>
    public async Task<List<VendorEntity>> ListAsync(bool includeInactive = false)
    {
        var vendors = new List<VendorEntity>();

        await foreach (var entity in _storage.Vendors
            .QueryAsync<VendorEntity>(e => e.PartitionKey == "Vendor"))
        {
            if (includeInactive || entity.IsActive)
            {
                vendors.Add(entity);
            }
        }

        vendors.Sort((a, b) => string.Compare(a.LegalName, b.LegalName, StringComparison.OrdinalIgnoreCase));

        _logger.LogDebug("Listed {Count} vendors (includeInactive: {IncludeInactive})", vendors.Count, includeInactive);
        return vendors;
    }

    /// <summary>
    /// Finds an active vendor by legal name (case-insensitive).
    /// Returns null if no matching active vendor is found.
    /// </summary>
    public async Task<VendorEntity?> FindByLegalNameAsync(string legalName)
    {
        var vendors = await ListAsync();
        return vendors.FirstOrDefault(v =>
            v.LegalName.Equals(legalName, StringComparison.OrdinalIgnoreCase));
    }
}
