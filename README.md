# RC Hackathon: AP Invoice Processing Agent

An intelligent Accounts Payable invoice processing agent that automates invoice ingestion, validation, exception handling, and batch posting to Zoho Books. Built for the Rapid Circle internal hackathon.

## Live App

| Item | Link |
|------|------|
| **Live URL** | https://gentle-grass-03ab54203.7.azurestaticapps.net |
| **MVP1 Actions** | [MVP1-ACTIONS.md](MVP1-ACTIONS.md) — Full checklist of what to build |
| **Specification** | [docs/SPECIFICATION.md](docs/SPECIFICATION.md) — Features F1-F6 |
| **KT Guide** | [KT-GUIDE.md](KT-GUIDE.md) — Code recipes, AI tools, Git commands |

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

## Getting Started

### Option A: GitHub Codespaces (Recommended — Zero Install)

1. Click **Code** → **Codespaces** → **Create codespace on main**
2. Wait ~2 minutes for the container to build (everything auto-installs)
3. In the terminal run: `bash scripts/swa-start.sh`
4. Click the **"Open in Browser"** notification for port **4280**
5. Click **Seed Test Data** on the homepage
6. Login with any demo persona, MFA code: **123456**

**No local installs needed. Works in any browser.**

### Option B: Local Development

**Prerequisites:**
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 22.x](https://nodejs.org/)
- `npm install -g azure-functions-core-tools@4 @azure/static-web-apps-cli azurite`

```bash
# 1. Clone
git clone https://github.com/RapidCircle/rc-hackathon-ap-invoice-agent.git
cd rc-hackathon-ap-invoice-agent

# 2. Setup local config
bash tools/init-local-settings.sh

# 3. Start
bash scripts/swa-start.sh

# 4. Open http://127.0.0.1:4280
# Click "Seed Test Data" → Login → MFA code: 123456
```

### Seed Demo Data

On the landing page, click **Seed Test Data**. This creates:
- **10 vendors** — TCS, Infosys, Wipro, Tech Mahindra, HCL, Mindtree, Mphasis, L&T Infotech, Persistent, Zensar
- **20 invoices** — 5 Received, 5 ReadyForZoho, 5 Exception, 3 InReview, 2 Corrected

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
