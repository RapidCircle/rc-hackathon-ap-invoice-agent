using Api.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Api.Services;

/// <summary>
/// Seeds demo AP invoice data (vendors, invoices) into Azure Table Storage.
/// Provides idempotent SeedAsync and destructive ResetAsync operations.
/// </summary>
public class AppDataSeeder
{
    private readonly TableStorageContext _storage;
    private readonly ILogger<AppDataSeeder> _logger;

    public AppDataSeeder(TableStorageContext storage, ILogger<AppDataSeeder> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// Seeds demo vendor and invoice data if not already present (idempotent).
    /// Creates 10 Indian vendor companies and 20 sample invoices with varied statuses.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _storage.EnsureTablesExistAsync();

        // Check if vendors already exist
        var existingVendors = new List<VendorEntity>();
        await foreach (var v in _storage.Vendors.QueryAsync<VendorEntity>(
            e => e.PartitionKey == "Vendor", cancellationToken: cancellationToken))
        {
            existingVendors.Add(v);
            if (existingVendors.Count > 0) break;
        }

        if (existingVendors.Count > 0)
        {
            _logger.LogInformation("Demo AP data already seeded, skipping");
            return;
        }

        var now = DateTime.UtcNow;

        // Seed vendors
        var vendors = CreateDemoVendors(now);
        foreach (var vendor in vendors)
        {
            await _storage.Vendors.UpsertEntityAsync(vendor, TableUpdateMode.Replace, cancellationToken);
        }
        _logger.LogInformation("Seeded {Count} demo vendors", vendors.Count);

        // Seed invoices
        var invoices = CreateDemoInvoices(vendors, now);
        foreach (var invoice in invoices)
        {
            await _storage.Invoices.UpsertEntityAsync(invoice, TableUpdateMode.Replace, cancellationToken);
        }
        _logger.LogInformation("Seeded {Count} demo invoices", invoices.Count);

        _logger.LogInformation("Demo AP data seeded successfully (10 vendors, 20 invoices)");
    }

    /// <summary>
    /// Clears all demo AP data and re-seeds.
    /// </summary>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _storage.EnsureTablesExistAsync();

        _logger.LogInformation("Resetting demo AP data...");

        await DeleteAllEntitiesAsync(_storage.Invoices, "Invoice", cancellationToken);
        await DeleteAllEntitiesAsync(_storage.Vendors, "Vendor", cancellationToken);
        await DeleteAllEntitiesAsync(_storage.Batches, "Batch", cancellationToken);

        _logger.LogInformation("Demo AP data cleared, re-seeding...");

        // Force re-seed by calling SeedAsync after clearing
        await SeedInternalAsync(cancellationToken);
    }

    /// <summary>
    /// Internal seed that always runs (does not check for existing data).
    /// </summary>
    private async Task SeedInternalAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var vendors = CreateDemoVendors(now);
        foreach (var vendor in vendors)
        {
            await _storage.Vendors.UpsertEntityAsync(vendor, TableUpdateMode.Replace, cancellationToken);
        }

        var invoices = CreateDemoInvoices(vendors, now);
        foreach (var invoice in invoices)
        {
            await _storage.Invoices.UpsertEntityAsync(invoice, TableUpdateMode.Replace, cancellationToken);
        }

        _logger.LogInformation("Re-seeded {VendorCount} vendors and {InvoiceCount} invoices", vendors.Count, invoices.Count);
    }

    #region Vendor Data

    /// <summary>
    /// Creates 10 Indian IT company vendor entities.
    /// </summary>
    private static List<VendorEntity> CreateDemoVendors(DateTime now)
    {
        var vendorData = new[]
        {
            ("Tata Consultancy Services", "TCS", "27AAACT2727Q1ZV"),
            ("Infosys Ltd", "Infosys", "29AABCI1234L1Z5"),
            ("Wipro Limited", "Wipro", "29AABCW1234M1Z6"),
            ("Tech Mahindra", "Tech M", "27AAACT5678R1ZW"),
            ("HCL Technologies", "HCL Tech", "09AABCH1234N1Z7"),
            ("Mindtree Ltd", "Mindtree", "29AABCM5678P1Z8"),
            ("Mphasis Ltd", "Mphasis", "29AABCM9012Q1Z9"),
            ("L&T Infotech", "LTI", "27AABCL3456R1ZA"),
            ("Persistent Systems", "Persistent", "27AABCP7890S1ZB"),
            ("Zensar Technologies", "Zensar", "27AABCZ1234T1ZC"),
        };

        return vendorData.Select((v, i) => new VendorEntity
        {
            PartitionKey = "Vendor",
            RowKey = $"vendor-{(i + 1):D3}",
            LegalName = v.Item1,
            TradingName = v.Item2,
            TaxId = v.Item3,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();
    }

    #endregion

    #region Invoice Data

    /// <summary>
    /// Creates 20 sample invoices with varied statuses and realistic Indian company data.
    /// Status distribution: 5 Received, 5 ReadyForZoho, 5 Exception, 3 InReview, 2 Corrected.
    /// </summary>
    private static List<InvoiceEntity> CreateDemoInvoices(List<VendorEntity> vendors, DateTime now)
    {
        var invoices = new List<InvoiceEntity>();

        // --- 5 Received (new, unprocessed) ---
        invoices.Add(CreateInvoice("inv-001", vendors[0].LegalName, "TCS/2026/INV-4501",
            D(2026, 4, 1), "INR", 2450000.00, 441000.00, "PO-2026-101",
            "[{\"description\":\"SAP S/4HANA Implementation - Phase 2\",\"amount\":2450000}]",
            InvoiceStatus.Received, "", "",
            "accounts@tcs.com", "Invoice TCS/2026/INV-4501 for SAP Implementation", D(2026, 4, 2),
            "TCS_INV_4501.pdf", 0.94, now));

        invoices.Add(CreateInvoice("inv-002", vendors[1].LegalName, "INF-2026-03-7821",
            D(2026, 3, 28), "INR", 1875000.00, 337500.00, "PO-2026-102",
            "[{\"description\":\"Cloud Migration Services - Azure\",\"amount\":1250000},{\"description\":\"DevOps Setup & Training\",\"amount\":625000}]",
            InvoiceStatus.Received, "", "",
            "billing@infosys.com", "March 2026 Invoice - Cloud Migration", D(2026, 4, 1),
            "Infosys_Invoice_7821.pdf", 0.91, now));

        invoices.Add(CreateInvoice("inv-003", vendors[2].LegalName, "WIP/FY26/00892",
            D(2026, 4, 5), "INR", 3200000.00, 576000.00, "PO-2026-103",
            "[{\"description\":\"Managed Security Operations Center - Q1\",\"amount\":3200000}]",
            InvoiceStatus.Received, "", "",
            "finance@wipro.com", "Wipro SOC Services Invoice Q1 2026", D(2026, 4, 6),
            "Wipro_SOC_Q1_2026.pdf", 0.96, now));

        invoices.Add(CreateInvoice("inv-004", vendors[3].LegalName, "TM-INV-2026-1134",
            D(2026, 4, 8), "USD", 45000.00, 0.00, "PO-2026-104",
            "[{\"description\":\"Salesforce CRM Customization\",\"amount\":30000},{\"description\":\"User Training (Remote)\",\"amount\":15000}]",
            InvoiceStatus.Received, "", "",
            "invoices@techmahindra.com", "Salesforce Project Invoice - April 2026", D(2026, 4, 9),
            "TechM_SF_Invoice_1134.pdf", 0.89, now));

        invoices.Add(CreateInvoice("inv-005", vendors[4].LegalName, "HCL-2026-APR-0056",
            D(2026, 4, 10), "INR", 4750000.00, 855000.00, "PO-2026-105",
            "[{\"description\":\"ERP Maintenance & Support - Annual\",\"amount\":4750000}]",
            InvoiceStatus.Received, "", "",
            "accounts.payable@hcltech.com", "Annual ERP Support Contract Invoice", D(2026, 4, 11),
            "HCL_ERP_Support_2026.pdf", 0.93, now));

        // --- 5 ReadyForZoho (validated, all fields pass) ---
        invoices.Add(CreateInvoice("inv-006", vendors[0].LegalName, "TCS/2026/INV-4480",
            D(2026, 3, 15), "INR", 1650000.00, 297000.00, "PO-2026-090",
            "[{\"description\":\"Data Analytics Platform Development\",\"amount\":1650000}]",
            InvoiceStatus.ReadyForZoho, "", "",
            "accounts@tcs.com", "Invoice TCS/2026/INV-4480 - Analytics Platform", D(2026, 3, 16),
            "TCS_INV_4480.pdf", 0.97, now));

        invoices.Add(CreateInvoice("inv-007", vendors[1].LegalName, "INF-2026-02-7654",
            D(2026, 2, 28), "INR", 980000.00, 176400.00, "PO-2026-078",
            "[{\"description\":\"API Gateway Implementation\",\"amount\":650000},{\"description\":\"Load Testing & Optimization\",\"amount\":330000}]",
            InvoiceStatus.ReadyForZoho, "", "",
            "billing@infosys.com", "February Invoice - API Gateway Project", D(2026, 3, 1),
            "Infosys_Invoice_7654.pdf", 0.95, now));

        invoices.Add(CreateInvoice("inv-008", vendors[5].LegalName, "MT-2026-Q1-0234",
            D(2026, 3, 20), "INR", 520000.00, 93600.00, "PO-2026-085",
            "[{\"description\":\"UI/UX Redesign - Customer Portal\",\"amount\":520000}]",
            InvoiceStatus.ReadyForZoho, "", "",
            "finance@mindtree.com", "Customer Portal Redesign Invoice", D(2026, 3, 21),
            "Mindtree_Portal_Q1.pdf", 0.92, now));

        invoices.Add(CreateInvoice("inv-009", vendors[6].LegalName, "MPH-INV-26-03-445",
            D(2026, 3, 25), "INR", 1120000.00, 201600.00, "PO-2026-088",
            "[{\"description\":\"Infrastructure Monitoring Setup\",\"amount\":720000},{\"description\":\"24x7 NOC Services - March\",\"amount\":400000}]",
            InvoiceStatus.ReadyForZoho, "", "",
            "ap@mphasis.com", "March Services Invoice - Infrastructure", D(2026, 3, 26),
            "Mphasis_Infra_Mar2026.pdf", 0.96, now));

        invoices.Add(CreateInvoice("inv-010", vendors[8].LegalName, "PS-2026-0312",
            D(2026, 3, 12), "EUR", 28500.00, 5415.00, "PO-2026-082",
            "[{\"description\":\"IoT Platform Integration - EU Client\",\"amount\":28500}]",
            InvoiceStatus.ReadyForZoho, "", "",
            "billing@persistent.com", "IoT Integration Invoice - EU Project", D(2026, 3, 13),
            "Persistent_IoT_EU.pdf", 0.90, now));

        // --- 5 Exception (various failure reasons) ---
        invoices.Add(CreateInvoice("inv-011", "Accenture India Pvt Ltd", "ACC-2026-IND-0891",
            D(2026, 4, 3), "INR", 2100000.00, 378000.00, "PO-2026-106",
            "[{\"description\":\"Digital Transformation Consulting\",\"amount\":2100000}]",
            InvoiceStatus.Exception, "Vendor 'Accenture India Pvt Ltd' not found in active vendor master.", "",
            "invoices@accenture.com", "Digital Transformation - April Invoice", D(2026, 4, 4),
            "Accenture_DT_Apr2026.pdf", 0.88, now));

        invoices.Add(CreateInvoice("inv-012", vendors[0].LegalName, "TCS/2026/INV-4501",
            D(2026, 4, 1), "INR", 2450000.00, 441000.00, "PO-2026-101",
            "[{\"description\":\"SAP S/4HANA Implementation - Phase 2\",\"amount\":2450000}]",
            InvoiceStatus.Exception, "Duplicate invoice: vendor 'Tata Consultancy Services' already has invoice 'TCS/2026/INV-4501'.", "",
            "accounts@tcs.com", "Invoice TCS/2026/INV-4501 - Resend", D(2026, 4, 5),
            "TCS_INV_4501_resend.pdf", 0.94, now));

        invoices.Add(CreateInvoice("inv-013", vendors[2].LegalName, "WIP/FY26/00910",
            D(2026, 4, 7), "INR", 1500000.00, 270000.00, "",
            "[{\"description\":\"Network Infrastructure Upgrade\",\"amount\":1500000}]",
            InvoiceStatus.Exception, "Purchase order number is required.", "",
            "finance@wipro.com", "Network Upgrade Invoice - April", D(2026, 4, 8),
            "Wipro_Network_Apr2026.pdf", 0.85, now));

        invoices.Add(CreateInvoice("inv-014", vendors[4].LegalName, "HCL-2026-APR-0060",
            D(2026, 4, 12), "XYZ", 890000.00, 160200.00, "PO-2026-108",
            "[{\"description\":\"Cybersecurity Audit Services\",\"amount\":890000}]",
            InvoiceStatus.Exception, "Currency 'XYZ' is not a recognized ISO 4217 code.", "",
            "accounts.payable@hcltech.com", "Cybersecurity Audit Invoice", D(2026, 4, 13),
            "HCL_CyberAudit_Apr2026.pdf", 0.78, now));

        invoices.Add(CreateInvoice("inv-015", vendors[7].LegalName, "LTI-2026-04-0078",
            D(2026, 4, 14), "INR", -50000.00, 0.00, "PO-2026-109",
            "[{\"description\":\"Credit Note - Overcharge Adjustment\",\"amount\":-50000}]",
            InvoiceStatus.Exception, "Total amount must be greater than zero.", "",
            "finance@ltimindtree.com", "Credit Note LTI-2026-04-0078", D(2026, 4, 15),
            "LTI_CreditNote_0078.pdf", 0.82, now));

        // --- 3 InReview (admin is correcting) ---
        invoices.Add(CreateInvoice("inv-016", vendors[3].LegalName, "TM-INV-2026-1098",
            D(2026, 3, 30), "INR", 760000.00, 136800.00, "PO-2026-092",
            "[{\"description\":\"ServiceNow ITSM Configuration\",\"amount\":760000}]",
            InvoiceStatus.InReview, "Vendor name mismatch detected during extraction.", "Admin reviewing vendor name - may be a subsidiary.",
            "invoices@techmahindra.com", "ServiceNow Configuration Invoice", D(2026, 3, 31),
            "TechM_ITSM_Mar2026.pdf", 0.83, now));

        invoices.Add(CreateInvoice("inv-017", vendors[9].LegalName, "ZEN-2026-Q1-0156",
            D(2026, 3, 22), "INR", 430000.00, 77400.00, "PO-2026-086",
            "[{\"description\":\"Test Automation Framework Setup\",\"amount\":280000},{\"description\":\"QA Resource Augmentation - March\",\"amount\":150000}]",
            InvoiceStatus.InReview, "PO number could not be verified against procurement system.", "Checking with procurement team for PO confirmation.",
            "billing@zensar.com", "QA Services Invoice - Q1 2026", D(2026, 3, 23),
            "Zensar_QA_Q1_2026.pdf", 0.87, now));

        invoices.Add(CreateInvoice("inv-018", vendors[5].LegalName, "MT-2026-Q1-0267",
            D(2026, 4, 2), "INR", 195000.00, 35100.00, "PO-2026-095",
            "[{\"description\":\"Mobile App Bug Fixes - Sprint 14\",\"amount\":195000}]",
            InvoiceStatus.InReview, "Tax amount discrepancy detected (expected 18% GST).", "Verifying GST calculation with vendor.",
            "finance@mindtree.com", "Sprint 14 Bug Fix Invoice", D(2026, 4, 3),
            "Mindtree_Sprint14_Fixes.pdf", 0.91, now));

        // --- 2 Corrected (fixed by admin) ---
        invoices.Add(CreateInvoice("inv-019", vendors[7].LegalName, "LTI-2026-03-0065",
            D(2026, 3, 18), "INR", 1340000.00, 241200.00, "PO-2026-083",
            "[{\"description\":\"Power BI Dashboard Development\",\"amount\":840000},{\"description\":\"Data Warehouse Optimization\",\"amount\":500000}]",
            InvoiceStatus.Corrected, "", "Vendor name corrected from 'LTI Infotech' to 'L&T Infotech'. Re-validated successfully.",
            "finance@ltimindtree.com", "BI & Data Services Invoice - March", D(2026, 3, 19),
            "LTI_BI_Data_Mar2026.pdf", 0.93, now));

        invoices.Add(CreateInvoice("inv-020", vendors[8].LegalName, "PS-2026-0298",
            D(2026, 3, 10), "INR", 675000.00, 121500.00, "PO-2026-080",
            "[{\"description\":\"Microservices Architecture Consulting\",\"amount\":450000},{\"description\":\"Code Review & Refactoring\",\"amount\":225000}]",
            InvoiceStatus.Corrected, "", "Currency corrected from USD to INR. PO number updated after procurement confirmation.",
            "billing@persistent.com", "Architecture Consulting Invoice", D(2026, 3, 11),
            "Persistent_Arch_Mar2026.pdf", 0.89, now));

        return invoices;
    }

    /// <summary>
    /// Factory method for creating invoice entities with all fields populated.
    /// </summary>
    private static InvoiceEntity CreateInvoice(
        string rowKey, string vendorLegalName, string invoiceNumber,
        DateTime invoiceDate, string currency, double totalAmount, double taxAmount,
        string poNumber, string lineItems, string status,
        string exceptionReason, string exceptionNotes,
        string emailSender, string emailSubject, DateTime emailReceivedDate,
        string attachmentName, double extractionConfidence, DateTime now)
    {
        return new InvoiceEntity
        {
            PartitionKey = "Invoice",
            RowKey = rowKey,
            VendorLegalName = vendorLegalName,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = invoiceDate,
            InvoiceCurrency = currency,
            TotalAmount = totalAmount,
            TaxAmount = taxAmount,
            PoNumber = poNumber,
            LineItemsSummary = lineItems,
            Status = status,
            ExceptionReason = exceptionReason,
            ExceptionNotes = exceptionNotes,
            EmailSender = emailSender,
            EmailSubject = emailSubject,
            EmailReceivedDate = emailReceivedDate,
            AttachmentName = attachmentName,
            ExtractionConfidence = extractionConfidence,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "demo-seeder",
            UpdatedBy = "demo-seeder"
        };
    }

    /// <summary>
    /// Helper to create UTC DateTime values concisely.
    /// </summary>
    private static DateTime D(int year, int month, int day)
    {
        return DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);
    }

    #endregion

    #region Delete Helpers

    /// <summary>
    /// Deletes all entities with the given partition key from a table.
    /// </summary>
    private async Task DeleteAllEntitiesAsync(TableClient table, string partitionKey, CancellationToken ct)
    {
        var entities = new List<ITableEntity>();
        await foreach (var entity in table.QueryAsync<TableEntity>(
            e => e.PartitionKey == partitionKey, cancellationToken: ct))
        {
            entities.Add(entity);
        }

        foreach (var entity in entities)
        {
            try
            {
                await table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: ct);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Already deleted
            }
        }

        _logger.LogInformation("Deleted {Count} entities from partition '{PartitionKey}'", entities.Count, partitionKey);
    }

    #endregion
}
