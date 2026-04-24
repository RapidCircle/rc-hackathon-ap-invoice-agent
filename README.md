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

| Layer | Technology | Details |
|---|---|---|
| Backend | .NET 8 Azure Functions (isolated worker) | C# API with CRUD, validation, batching |
| Storage | Azure Table Storage | Azurite emulator for local dev, Azure Storage in production |
| AI/ML | Azure AI Document Intelligence | **Already provisioned and integrated** (see below) |
| Frontend | Static Web App (HTML/CSS/JS) | Tailwind CSS, vanilla JS (ES5 compatible) |
| Auth | SWA built-in auth | Mock auth for local dev (MFA code: 123456) |
| Target ERP | Zoho Books | Mock integration (ready for real API) |
| Hosting | Azure Static Web Apps | Auto-deployed via GitHub Actions (manual trigger) |

## Features

- **Invoice Ingestion** — Receive invoices via email simulation, extract data using AI Document Intelligence
- **8-Field Validation** — Visual pipeline stepper showing pass/fail for each of 8 mandatory fields
- **Create Invoice** — Full form with vendor selection, all fields, and file attachment
- **File Upload & Preview** — Drag-and-drop PDF/PNG/JPG upload with inline preview
- **Exception Management** — Flag invalid invoices, provide admin review and correction workflow
- **Vendor Master** — Maintain active vendor list with legal names, trading names, and tax IDs
- **Batch Processing** — Group validated invoices by cutoff date and push to Zoho Books (mock)
- **Dashboard Stats** — Real-time counts by status (Received, ReadyForZoho, Exception, InReview)
- **Demo Data** — One-click seed with 10 Indian IT vendors and 20 realistic invoices
- **Test Invoice Files** — 5 professional HTML invoice templates in `test-invoices/` (save as PDF to test upload)

## Azure AI Document Intelligence (Already Integrated)

The AI extraction service is **already provisioned and configured**. No setup needed.

| Setting | Value |
|---|---|
| Resource | `ai-hackathon-docintel` (Form Recognizer F0 - Free tier) |
| Endpoint | `https://ai-hackathon-docintel-fe537.cognitiveservices.azure.com/` |
| Model | `prebuilt-invoice` (extracts vendor, amount, date, line items, etc.) |
| Free Quota | 500 pages/month |

**How it works:**
- The endpoint and key are already set as **environment variables** in the deployed Azure Static Web App
- For local development, the endpoint is pre-configured in `api/local.settings.template.json`
- The key is stored as a GitHub secret and Azure SWA app setting — you don't need to manage it
- Currently the extraction service uses **mock data** for the hackathon demo (see `api/Services/ExtractionService.cs`)
- To switch to real AI extraction, uncomment the real implementation in `ExtractionService.cs` — the code is already written

**Environment variables used by the API:**
```
AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT   → Already set in Azure SWA app settings
AZURE_DOCUMENT_INTELLIGENCE_KEY        → Already set in Azure SWA app settings
STORAGE                                → Azure Table Storage connection string (already set)
```

## Getting Started

### Option A: GitHub Codespaces (Recommended — Zero Install)

1. Go to the repo: https://github.com/RapidCircle/rc-hackathon-ap-invoice-agent
2. Click **Code** → **Codespaces** → **Create codespace on main**
3. Wait **3-5 minutes** for first-time container build (installs .NET 8, Node 22, Azure CLI, Chromium, Redis — subsequent starts are ~30 seconds)
4. The "paid for by" message is normal — Codespaces has free hours, no charges for hackathon usage
5. Once the VS Code editor loads in your browser, open the **Terminal** (Ctrl+`)
6. Run:
   ```bash
   bash scripts/swa-start.sh
   ```
7. Wait for the green message: `All services ready`
8. Click the **"Open in Browser"** popup for port **4280** (or go to Ports tab → click 4280)
9. On the landing page, click **Seed Test Data** to load demo invoices
10. Click **Sign In** → pick any demo persona → MFA code: **123456**
11. You're in! Navigate to Dashboard, Invoices, or Vendors

**What's running locally in Codespace:**
- `http://localhost:4280` — SWA proxy (frontend + API)
- `http://localhost:7071` — Azure Functions API
- `http://localhost:10000-10002` — Azurite (local storage emulator)

### Option B: Local Development

**Prerequisites:**
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 22.x](https://nodejs.org/)
- Install CLI tools: `npm install -g azure-functions-core-tools@4 @azure/static-web-apps-cli azurite`

```bash
# 1. Clone
git clone https://github.com/RapidCircle/rc-hackathon-ap-invoice-agent.git
cd rc-hackathon-ap-invoice-agent

# 2. Setup local config (creates api/local.settings.json from template)
bash tools/init-local-settings.sh

# 3. Start everything (builds .NET, starts Azurite + SWA CLI)
bash scripts/swa-start.sh

# 4. Open http://127.0.0.1:4280
# Click "Seed Test Data" → Sign In → MFA code: 123456
```

**Troubleshooting local dev:**
- If port 4280 is busy: `bash scripts/swa-start.sh --restart`
- If build fails: `dotnet build api/api.csproj` to see errors
- Logs: `tail -50 /tmp/swa-start.log`
- Stop everything: `bash scripts/swa-start.sh --stop`

### Seed Demo Data

On the landing page, click **Seed Test Data**. This creates:
- **10 vendors** — TCS, Infosys, Wipro, Tech Mahindra, HCL, Mindtree, Mphasis, L&T Infotech, Persistent, Zensar
- **20 invoices** — 5 Received, 5 ReadyForZoho, 5 Exception, 3 InReview, 2 Corrected

### Test Invoice Files

The `test-invoices/` folder contains 5 professional HTML invoices:
- Open any `.html` file in your browser
- Press **Ctrl+P** → **Save as PDF**
- Upload the PDF using the **+ Create Invoice** button on the Invoices page

### Deploying to Azure

Deployments use **GitHub Actions** with **manual trigger only** (no auto-deploy on push):
1. Go to **Actions** tab in GitHub
2. Click **Deploy to Azure Static Web Apps**
3. Click **Run workflow** → select `main` branch → **Run**
4. Wait ~4 minutes for build + deploy
5. Live at: https://gentle-grass-03ab54203.7.azurestaticapps.net

### Using AI Coding Tools

This project is designed for **vibe coding** with AI tools:
- **GitHub Copilot** — Already configured (see `.github/copilot-instructions.md`)
- **Claude Code** — Works in terminal within Codespace
- **Cursor / Windsurf** — Clone locally and open

The `.github/copilot-instructions.md` file tells AI tools about the project architecture, safe patterns, and what to avoid.

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
| GET | `/api/invoice-stats` | Dashboard statistics |
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
