# AP Invoice Intake Agent — Project Brief

## What Is This Project?

This is an **Accounts Payable (AP) Invoice Processing Agent** — an AI-powered web application that automates the entire invoice lifecycle for a company's finance team.

**The Problem:** Companies receive hundreds of invoices from vendors via email. Finance teams manually read each invoice, type data into spreadsheets, check for errors, and enter it into their accounting system (Zoho Books). This is slow, error-prone, and tedious.

**The Solution:** This app uses **Azure AI Document Intelligence** to automatically extract data from invoice PDFs/images, validates the extracted data against 8 mandatory business rules, flags exceptions for human review, and pushes validated invoices to the accounting system.

## How It Works (End-to-End Flow)

```
Vendor sends invoice PDF via email
        |
        v
[1. INGEST] → Invoice PDF arrives, stored in Azure Blob Storage
        |
        v
[2. EXTRACT] → AI Document Intelligence reads the PDF and extracts:
                - Vendor Legal Name
                - Invoice Number
                - Invoice Date
                - Currency (INR, USD, EUR, etc.)
                - Total Amount (incl. tax)
                - Tax Amount (GST)
                - PO Number
                - Line Items
        |
        v
[3. VALIDATE] → 8 mandatory checks run automatically:
                 ✓ Vendor exists in vendor master?
                 ✓ Invoice number present?
                 ✓ Date valid?
                 ✓ Currency is valid ISO code?
                 ✓ Amount > 0?
                 ✓ Tax amount >= 0?
                 ✓ PO number present?
                 ✓ Not a duplicate?
        |
        v
[4. ROUTE] → All passed? → Status: "ReadyForZoho" (ready to post)
             Failed?     → Status: "Exception" (needs human review)
        |
        v
[5. REVIEW] → Finance admin reviews exceptions, corrects data, re-validates
        |
        v
[6. PUSH] → Batch validated invoices → Push to Zoho Books (accounting system)
```

## What's Already Built (Your Starting Point)

| Feature | Status | Where |
|---------|--------|-------|
| Landing page with branding | Done | `index.html` |
| Login with mock auth | Done | `login.html`, `js/authService.js` |
| Dashboard with stats cards | Done | `app/dashboard.html` |
| Invoice list with search | Done | `app/invoices.html` |
| Create Invoice form (10 fields) | Done | Modal in `app/invoices.html` |
| Invoice detail with 8-field display | Done | `app/invoice-detail.html` |
| Validation pipeline stepper (visual) | Done | 8-step pass/fail UI |
| File upload with PDF/image preview | Done | Drag-and-drop + inline preview |
| Vendor master (CRUD) | Done | `app/vendors.html` |
| Backend API (full CRUD) | Done | `api/Functions/*.cs` |
| Validation service (8 checks) | Done | `api/Services/ValidationService.cs` |
| AI extraction service (mock) | Done | `api/Services/ExtractionService.cs` |
| Demo data seeder | Done | 10 vendors, 20 invoices |
| Azure AI Document Intelligence | Provisioned | Endpoint + key in env vars |
| Azure Static Web App hosting | Deployed | Live URL working |
| GitHub Actions CI/CD | Configured | Manual deploy trigger |
| Codespace devcontainer | Ready | One-click dev environment |

## What You Need To Build (Hackathon Goals)

### Priority 1 — Must Have for Demo

- [ ] **Switch AI extraction from mock to real** — Uncomment the real implementation in `api/Services/ExtractionService.cs` (code is already written, just commented out). Upload a real PDF and see AI extract the fields
- [ ] **Invoice file storage** — Store uploaded PDFs/images in Azure Blob Storage (connection string already configured). Show the stored file in invoice detail page
- [ ] **Improve the validation stepper UI** — Make it more visual with animations, show confidence scores from AI extraction
- [ ] **End-to-end demo flow** — Upload a real invoice PDF → AI extracts fields → validation runs → show results

### Priority 2 — Nice to Have

- [ ] **Edit extracted fields** — Let admin correct extracted data inline on invoice detail page
- [ ] **Batch processing UI** — Show batch creation, list batches, simulate Zoho push
- [ ] **Email simulation** — Simulate receiving invoices via email (create a form that mimics email input)
- [ ] **Dashboard charts** — Add a chart/graph to the dashboard (e.g., invoices by status, amount by vendor)
- [ ] **Vendor matching** — Fuzzy match extracted vendor name against vendor master

### Priority 3 — Stretch Goals

- [ ] **Real Zoho Books integration** — Connect to Zoho API
- [ ] **Multi-language invoice support** — Test with invoices in different languages
- [ ] **Audit trail** — Show history of status changes for each invoice
- [ ] **Export** — Download invoice data as CSV/Excel

## Key Files to Know

| File | What It Does |
|------|-------------|
| `api/Services/ExtractionService.cs` | **AI extraction** — mock now, real code commented out. This is your main AI integration point |
| `api/Services/ValidationService.cs` | 8-field validation logic |
| `api/Services/InvoiceService.cs` | CRUD operations for invoices (Table Storage) |
| `api/Functions/InvoiceFunctions.cs` | HTTP API endpoints for invoices |
| `api/Models/InvoiceEntity.cs` | Invoice data model (all fields) |
| `api/Services/AppDataSeeder.cs` | Demo data (10 vendors, 20 invoices) |
| `app/js/invoice-detail.js` | Invoice detail page logic (stepper, upload, validation) |
| `app/js/invoices.js` | Invoice list + Create Invoice form |
| `app/js/dashboard.js` | Dashboard stats and recent invoices |
| `api/local.settings.template.json` | Environment variables template |

## Azure Resources (Already Provisioned)

| Resource | Name | Purpose |
|----------|------|---------|
| Resource Group | `rg-hackathon-2026` | All resources |
| Static Web App | `swa-hackathon-team1-invoice` | Hosts frontend + API |
| Storage Account | `sthackteam1invoice` | Table Storage for data |
| AI Document Intelligence | `ai-hackathon-docintel` | Invoice PDF extraction (F0 free tier, 500 pages/month) |

**All environment variables (storage connection string, AI endpoint, AI key) are already configured in Azure SWA app settings. You don't need to set up anything.**

## How to Run Locally

```bash
# In GitHub Codespace terminal:
bash scripts/swa-start.sh

# Open port 4280 → Seed Test Data → Sign In (MFA: 123456)
```

## How to Deploy

1. Push your changes to `main` branch
2. Go to GitHub → Actions → "Deploy to Azure Static Web Apps" → Run workflow
3. Wait ~4 minutes → live at https://gentle-grass-03ab54203.7.azurestaticapps.net

## Team

| Member | Role |
|--------|------|
| Sagarika | AI extraction pipeline, Document Intelligence integration |
| Ameya | Frontend UI, dashboard, invoice detail page |
| Girish | Backend services, validation logic, vendor master |
