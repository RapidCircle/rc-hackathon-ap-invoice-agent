# AP Invoice Processing Agent - Specification

## Problem Statement

Rapid Circle's Accounts Payable team manually processes vendor invoices received via email. Each invoice must be validated against 8 mandatory fields, matched to the vendor master, checked for duplicates, and then posted to Zoho Books for payment processing. This manual process is error-prone, time-consuming, and creates bottlenecks during month-end closing.

The AP Invoice Processing Agent automates this workflow end-to-end: from email ingestion and AI-powered data extraction through validation, exception handling, and batch posting to Zoho.

## Target Users

| Role | Responsibilities |
|---|---|
| **AP Clerk** | Reviews incoming invoices, handles exceptions, corrects data |
| **AP Manager** | Monitors dashboard, approves batches, manages vendor master |
| **Finance Controller** | Views overall processing metrics and batch history |

## MVP Scope

### F1: Invoice Ingestion

**Goal:** Receive invoices from email attachments and extract structured data.

**Acceptance Criteria:**
- System can accept invoice documents (PDF, image) via API upload
- Azure AI Document Intelligence extracts key fields with confidence scores
- Extracted data is stored as an invoice record with status "Received"
- Email metadata (sender, subject, received date) is captured

### F2: 8-Field Mandatory Validation

**Goal:** Automatically validate each invoice against 8 mandatory fields.

**Mandatory Fields:**

| # | Field | Validation Rule |
|---|---|---|
| 1 | Vendor Legal Name | Must be present and match an active vendor in the vendor master |
| 2 | Invoice Number | Must be present; must be unique per vendor (duplicate detection) |
| 3 | Invoice Date | Must be a valid date |
| 4 | Invoice Currency | Must be a valid ISO 4217 currency code (INR, USD, EUR, etc.) |
| 5 | Total Amount | Must be greater than zero |
| 6 | Tax Amount | Must be zero or positive (cannot be negative) |
| 7 | PO Number | Must be present (purchase order reference required) |
| 8 | Duplicate Check | Same vendor + same invoice number = duplicate, flagged as Exception |

**Acceptance Criteria:**
- All 8 fields are checked on every validation run
- Invoices passing all checks move to "ReadyForZoho" status
- Invoices failing any check move to "Exception" status with clear error messages
- Validation can be triggered manually via API

### F3: Exception Handling

**Goal:** Provide a workflow for reviewing and correcting invalid invoices.

**Acceptance Criteria:**
- Exception invoices display the specific reason(s) for failure
- Admin can update any field on an exception invoice
- Admin can add notes during review (ExceptionNotes field)
- Admin can move invoice to "InReview" status while working on it
- After correction, invoice moves to "Corrected" status
- Corrected invoices can be re-validated to move to "ReadyForZoho"

### F4: Vendor Master Management

**Goal:** Maintain a list of approved vendors for validation matching.

**Acceptance Criteria:**
- CRUD operations for vendors (create, read, update, soft-delete)
- Each vendor has: Legal Name, Trading Name, Tax ID, Active status
- Vendor matching is case-insensitive on Legal Name
- Soft-delete deactivates vendor without removing records
- Only active vendors are used for invoice validation

### F5: Batch Processing

**Goal:** Group validated invoices into batches and push to Zoho Books.

**Acceptance Criteria:**
- Create a batch with a cutoff date/time
- All "ReadyForZoho" invoices created before cutoff are included
- Batch tracks: invoice count, total amount, invoice IDs
- Push batch to Zoho (mock integration for hackathon)
- Batch status transitions: Pending -> Pushed or Failed
- Invoice statuses update on successful push

### F6: Dashboard and Reporting

**Goal:** Provide real-time visibility into the invoice processing pipeline.

**Acceptance Criteria:**
- Count of invoices by status (Received, ReadyForZoho, Exception, InReview, Corrected)
- Total monetary amount across all invoices
- List view with filtering by status
- Individual invoice detail view

## Key Workflows

### Happy Path: Invoice to Zoho
1. Invoice arrives via email (simulated)
2. AI extracts fields from attachment
3. Invoice stored with status "Received"
4. Validation runs against 8 mandatory fields
5. All checks pass -> status becomes "ReadyForZoho"
6. Admin creates batch with cutoff date
7. Batch pushed to Zoho -> invoices marked "Pushed"

### Exception Path: Invalid Invoice
1. Invoice arrives and is extracted
2. Validation finds errors (e.g., vendor not in master)
3. Invoice status set to "Exception" with error details
4. AP Clerk reviews, sets status to "InReview"
5. Clerk corrects data (e.g., fixes vendor name)
6. Invoice status set to "Corrected"
7. Re-validation runs -> if clean, moves to "ReadyForZoho"

### Duplicate Detection
1. Invoice arrives with same vendor + invoice number as existing record
2. Validation catches duplicate
3. Invoice flagged as "Exception" with duplicate reason
4. Clerk investigates and either removes duplicate or updates invoice number

## Non-Functional Requirements

- All dates stored and processed in UTC
- API responses use camelCase JSON serialization
- Table Storage used for all persistence (no SQL database)
- Mock authentication for local development
- Demo data seeding for hackathon presentation
- Application Insights for telemetry and logging
