# AP Invoice Agent — MVP1 Action Checklist

## Deployment Info

| Item | Value |
|------|-------|
| **Live URL** | https://gentle-grass-03ab54203.7.azurestaticapps.net |
| **GitHub Repo** | https://github.com/RapidCircle/rc-hackathon-ap-invoice-agent |
| **CI/CD** | GitHub Actions → auto-deploys on push to `main` |
| **Azure SWA** | `swa-hackathon-team1-invoice` (rg-hackathon-2026, West Europe) |
| **Azure Storage** | `sthackteam1invoice` |
| **Codespaces** | Click "Code" → "Codespaces" → "Create codespace on main" |
| **Auth** | Mock auth (persona + MFA code: `123456`) |

---

## Team

| Member | Strengths | Suggested Focus |
|--------|-----------|-----------------|
| **Sagarika** | RAG / AI solutions | AI extraction pipeline, Document Intelligence integration |
| **Ameya** | — | Frontend UI, dashboard, invoice detail page |
| **Girish** | — | Backend services, validation logic, vendor master |

---

## MVP1 Feature Checklist

### STATUS: What's Already Built (Scaffold)

| Component | Status | Notes |
|-----------|--------|-------|
| Landing page (`index.html`) | Done | Seed data button, sign-in link |
| Login page (`login.html`) | Done | Mock auth with personas |
| Auth service (`js/authService.js`) | Done | Mock + SWA dual-mode |
| Dashboard page (`app/dashboard.html`) | Done | 4 stat cards + recent invoices table |
| Invoice list page (`app/invoices.html`) | Done | Table + upload modal + search |
| Invoice detail page (`app/invoice-detail.html`) | Done | Extracted fields, validation, actions |
| Vendor master page (`app/vendors.html`) | Done | CRUD table + add modal |
| Backend auth (mock) | Done | Login, MFA, personas |
| Invoice model (`InvoiceEntity.cs`) | Done | All 20+ fields |
| Vendor model (`VendorEntity.cs`) | Done | Legal name, trading name, tax ID |
| Batch model (`BatchEntity.cs`) | Done | Cutoff, status, invoice IDs |
| Enums + DTOs | Done | All status constants + request/response types |
| Invoice CRUD service | Done | Create, read, update, list, stats |
| Vendor CRUD service | Done | Create, read, update, soft-delete |
| Validation service | Done | 8-field validation + vendor match + duplicate check |
| Extraction service | Done | **Mock only** — returns fake extraction results |
| Batch service | Done | Create batch, mock push, list |
| All API endpoints | Done | 15 endpoints (invoices, vendors, batches, auth, demo) |
| Seed data service | Done | 10 Indian vendors + 20 sample invoices |
| CI/CD pipeline | Done | Auto-deploys to Azure on push |
| DevContainer / Codespaces | Done | Zero-install setup |

### What's NOT Working Yet (MVP1 Actions)

Below is every action needed to go from "scaffold" to "working MVP1 demo".
Prioritized by impact — do P0 first, then P1, then P2.

---

### P0 — Must Have for Demo (Do These First)

#### Action 1: Verify Seed Data Works End-to-End
- [ ] Open the live URL: https://gentle-grass-03ab54203.7.azurestaticapps.net
- [ ] Click "Seed Test Data" → should see success toast
- [ ] Login with any persona → MFA code `123456`
- [ ] Dashboard should show stats (5 Received, 5 ReadyForZoho, 5 Exception, etc.)
- [ ] Invoices page should list all 20 seeded invoices
- [ ] Click an invoice → detail page should show extracted fields
- [ ] Vendors page should show 10 vendors
- **If any of these fail**: Debug the backend service code and fix

#### Action 2: Fix Dashboard Stats API Call
- [ ] Open `app/js/dashboard.js`
- [ ] Verify it calls `GET /api/invoices/stats` correctly
- [ ] Verify stat cards update with real counts
- [ ] Verify recent invoices table renders
- **File**: `app/js/dashboard.js` → `app/dashboard.html`

#### Action 3: Fix Invoice List Search & Filtering
- [ ] Open `app/js/invoices.js`
- [ ] Verify search input filters the table in real-time
- [ ] Verify status filter dropdown works (if present)
- [ ] Verify clicking an invoice row navigates to detail page
- **File**: `app/js/invoices.js`

#### Action 4: Fix Invoice Detail Page Data Binding
- [ ] Open `app/js/invoice-detail.js`
- [ ] Verify it reads `?id=` from URL and fetches the invoice
- [ ] Verify all 8 extracted fields display correctly
- [ ] Verify validation results show pass/fail for each field
- [ ] Verify action buttons trigger the correct API calls:
  - "Mark Ready" → `PUT /api/invoices/{id}` with `status: "ReadyForZoho"`
  - "Flag Exception" → `PUT /api/invoices/{id}` with `status: "Exception"`
  - "Re-validate" → `POST /api/invoices/{id}/validate`
- **File**: `app/js/invoice-detail.js`

#### Action 5: Fix Vendor CRUD Operations
- [ ] Open `app/js/vendors.js`
- [ ] Verify "Add Vendor" modal creates vendor via `POST /api/vendors`
- [ ] Verify edit inline works via `PUT /api/vendors/{id}`
- [ ] Verify delete (soft) works via `DELETE /api/vendors/{id}`
- [ ] Verify vendor list refreshes after each operation
- **File**: `app/js/vendors.js`

---

### P1 — Important for Polished Demo

#### Action 6: Add Invoice Upload Flow
- [ ] "Upload Invoice" button on invoices page opens a modal
- [ ] Modal has: file picker (PDF/image) + vendor dropdown
- [ ] On submit: `POST /api/invoices` with form data
- [ ] After upload: auto-trigger extraction (`POST /api/invoices/{id}/validate`)
- [ ] Show extraction results with confidence scores
- [ ] Navigate to invoice detail page
- **Files**: `app/js/invoices.js` + `api/Functions/InvoiceFunctions.cs`

#### Action 7: Wire Batch Processing
- [ ] Add a "Batches" page or section to dashboard
- [ ] "Create Batch" button with cutoff date picker
- [ ] Calls `POST /api/batches` → shows batch with invoice count
- [ ] "Push to Zoho" button → calls `POST /api/batches/{id}/push`
- [ ] Shows push result (success/failure per invoice)
- **Files**: New page or dashboard section + `api/Functions/BatchFunctions.cs`

#### Action 8: Add Toast/Notification System
- [ ] Success/error toasts for all CRUD operations
- [ ] "Invoice validated successfully" → green toast
- [ ] "Validation failed: Missing vendor match" → red toast with reason
- [ ] Toast auto-dismisses after 5 seconds
- **Files**: All JS files

#### Action 9: Add Dashboard Chart (Chart.js)
- [ ] Add Chart.js CDN: `<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>`
- [ ] Donut chart showing invoice count by status
- [ ] Bar chart showing total amount by status
- [ ] Use design system colors: blue=Received, green=ReadyForZoho, red=Exception, amber=InReview
- **File**: `app/dashboard.html` + `app/js/dashboard.js`

#### Action 10: Add Status History / Audit Trail
- [ ] On invoice detail page, show status change history
- [ ] Each entry: timestamp, old status → new status, changed by
- [ ] Requires: add `statusHistory` JSON field to InvoiceEntity
- [ ] Backend: append to history on every status change
- **Files**: `api/Models/InvoiceEntity.cs` + `api/Services/InvoiceService.cs` + `app/js/invoice-detail.js`

---

### P2 — Nice to Have (If Time Permits)

#### Action 11: Connect Real Azure AI Document Intelligence
- [ ] Create Azure AI Document Intelligence resource (free tier: 500 pages/month)
- [ ] Get endpoint URL + API key
- [ ] Add to `api/local.settings.json`:
  ```json
  "AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT": "https://your-resource.cognitiveservices.azure.com/",
  "AZURE_DOCUMENT_INTELLIGENCE_KEY": "your-key"
  ```
- [ ] Uncomment the real implementation in `api/Services/ExtractionService.cs`
- [ ] Upload a real PDF invoice → see AI extract fields live
- **File**: `api/Services/ExtractionService.cs`

#### Action 12: Add Exception Email Notifications (Mock)
- [ ] When an invoice moves to "Exception" status, show a simulated email notification
- [ ] Display notification panel on dashboard: "Exception: Invoice INV-2024-005 from Wipro — Missing PO Number"
- [ ] No real email sending needed — just UI notification
- **Files**: `app/dashboard.html` + `app/js/dashboard.js`

#### Action 13: Add CSV Export
- [ ] "Export" button on invoices page
- [ ] Downloads all visible invoices as CSV
- [ ] Columns: Vendor, Invoice#, Date, Amount, Currency, Status
- [ ] Use the export recipe from KT-GUIDE.md
- **File**: `app/js/invoices.js`

#### Action 14: Mobile Responsive Polish
- [ ] Test all pages on mobile viewport (375px)
- [ ] Fix any overflow/layout issues
- [ ] Make tables horizontally scrollable
- [ ] Stack cards vertically on mobile
- **Files**: All HTML files — Tailwind responsive classes

#### Action 15: Dark Mode
- [ ] Add dark mode toggle to nav bar
- [ ] Use the dark mode recipe from KT-GUIDE.md
- **Files**: All HTML files + small JS toggle

---

## Demo Script (7 minutes)

Use this flow for your hackathon demo:

### 1. The Story (1 min)
> "Invoices arrive by email. Today they're manually processed — some get missed, some have errors that surface late. Our agent automates the entire flow."

### 2. Live Demo (4 min)
1. **Dashboard** → Show the 4 status cards with real counts
2. **Upload an invoice** → Show the upload flow, watch AI extract fields
3. **Validation** → Show an invoice that passed all 8 checks (green) vs one that failed (red with reasons)
4. **Exception handling** → Click an exception invoice → edit a field → re-validate → watch it turn green
5. **Vendor master** → Show the vendor list, add a new vendor
6. **Batch push** → Create a batch → push to Zoho → show success

### 3. AI Tools Used (1 min)
> "We used [Cursor/Gemini CLI/Copilot] to generate the frontend pages, the validation logic, and the Chart.js dashboard. The AI wrote ~80% of the code, we guided and fixed."

### 4. What's Next (1 min)
> "Real email monitoring, real Zoho integration, automated scheduling for Tuesday/Thursday batches, and Power BI dashboard for trends."

---

## Key File Reference

| What | File Path |
|------|-----------|
| Landing page | `index.html` |
| Login page | `login.html` |
| Dashboard | `app/dashboard.html` + `app/js/dashboard.js` |
| Invoice list | `app/invoices.html` + `app/js/invoices.js` |
| Invoice detail | `app/invoice-detail.html` + `app/js/invoice-detail.js` |
| Vendor master | `app/vendors.html` + `app/js/vendors.js` |
| Auth service | `js/authService.js` |
| Invoice API | `api/Functions/InvoiceFunctions.cs` |
| Vendor API | `api/Functions/VendorFunctions.cs` |
| Batch API | `api/Functions/BatchFunctions.cs` |
| Invoice logic | `api/Services/InvoiceService.cs` |
| Validation | `api/Services/ValidationService.cs` |
| AI extraction | `api/Services/ExtractionService.cs` |
| Seed data | `api/Services/AppDataSeeder.cs` |
| Design system | `docs/DESIGN.md` (TBD) |
| Requirements | `docs/SPECIFICATION.md` |
| Architecture doc | `docs/AP-Invoice-Intake-Requirements-and-Architecture.docx` |
| Code recipes | `KT-GUIDE.md` |
