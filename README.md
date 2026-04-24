# RC Hackathon: AP Invoice Processing Agent

An intelligent Accounts Payable invoice processing agent that automates invoice ingestion, validation, exception handling, and batch posting to Zoho Books. Built for the Rapid Circle internal hackathon.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8 Azure Functions (isolated worker) |
| Storage | Azure Table Storage (via Azurite for local dev) |
| AI/ML | Azure AI Document Intelligence (Form Recognizer) |
| Frontend | Static Web App (HTML/JS) |
| Auth | SWA built-in auth (mock auth for local dev) |
| Target ERP | Zoho Books (mock integration) |

## Features

- **Invoice Ingestion** — Receive invoices via email simulation, extract data using AI Document Intelligence
- **8-Field Validation** — Automated validation against vendor master, duplicate detection, mandatory field checks
- **Exception Management** — Flag invalid invoices, provide admin review and correction workflow
- **Vendor Master** — Maintain active vendor list with legal names, trading names, and tax IDs
- **Batch Processing** — Group validated invoices by cutoff date and push to Zoho Books (mock)
- **Dashboard Stats** — Real-time counts by status, total amounts, and processing metrics
- **Demo Data** — One-click seed with 10 Indian IT vendors and 20 realistic invoices

## Setup Instructions

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- Azure Static Web Apps CLI (`npm install -g @azure/static-web-apps-cli`)
- Azurite (Azure Storage Emulator): `npm install -g azurite`

### Quick Start

```bash
# 1. Clone the repository
git clone <repo-url>
cd rc-hackathon-ap-invoice-agent

# 2. Initialize local settings
cp api/local.settings.template.json api/local.settings.json

# 3. Start Azurite (in a separate terminal)
azurite --silent --location .azurite --debug .azurite/debug.log

# 4. Start the app with SWA CLI
swa start
```

### Seed Demo Data

After starting the app, seed sample data:

```bash
curl -X POST http://localhost:7071/api/demo/seed
```

To reset and re-seed:

```bash
curl -X POST http://localhost:7071/api/demo/reset
```

## Architecture

```
+-------------------+     +---------------------+     +------------------+
|   Email Inbox     |---->|  Extraction Service  |---->|  Invoice Store   |
|   (simulated)     |     |  (AI Doc Intel)      |     |  (Table Storage) |
+-------------------+     +---------------------+     +------------------+
                                                              |
                                                              v
+-------------------+     +---------------------+     +------------------+
|   Vendor Master   |<----|  Validation Service  |<----|  Invoice API     |
|   (Table Storage) |     |  (8 mandatory fields)|     |  (Azure Funcs)   |
+-------------------+     +---------------------+     +------------------+
                                                              |
                                    +-------------------------+
                                    |                         |
                                    v                         v
                          +------------------+     +------------------+
                          |  Exception Queue  |     |  Batch Service   |
                          |  (admin review)   |     |  (Zoho push)     |
                          +------------------+     +------------------+
```

## API Reference

| Method | Route | Description |
|---|---|---|
| GET | `/api/invoices` | List invoices (optional `?status=` filter) |
| GET | `/api/invoices/stats` | Dashboard statistics |
| GET | `/api/invoices/{id}` | Get invoice by ID |
| POST | `/api/invoices` | Create new invoice |
| PUT | `/api/invoices/{id}` | Update invoice |
| POST | `/api/invoices/{id}/validate` | Run validation, update status |
| GET | `/api/vendors` | List active vendors |
| POST | `/api/vendors` | Create vendor |
| PUT | `/api/vendors/{id}` | Update vendor |
| DELETE | `/api/vendors/{id}` | Soft-delete vendor |
| GET | `/api/batches` | List batches |
| POST | `/api/batches` | Create batch from ReadyForZoho invoices |
| POST | `/api/batches/{id}/push` | Push batch to Zoho (mock) |
| POST | `/api/demo/seed` | Seed demo data |
| POST | `/api/demo/reset` | Reset and re-seed demo data |
| GET | `/api/auth/mode` | Get authentication mode |
| GET | `/api/auth/me` | Get current user |

## Invoice Statuses

| Status | Description |
|---|---|
| `Received` | Newly ingested, not yet validated |
| `ReadyForZoho` | Passed all validation checks, eligible for batch |
| `Exception` | Failed validation (missing vendor, duplicate, bad fields) |
| `InReview` | Admin is reviewing and correcting the invoice |
| `Corrected` | Fixed by admin, ready for re-validation |

## License

Internal hackathon project - Rapid Circle.
