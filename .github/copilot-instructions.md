# Copilot Coding Agent Instructions

> **Purpose**: These instructions define **how coding agents must work
> in this repository**. They prioritise safety, determinism, fast
> feedback, and correctness in a **VS Code Dev Container + Azure Static
> Web Apps (SWA) + Azurite** development environment.

# 🔐 Golden Rules (READ FIRST)

These are the **highest-priority rules**. Violating them will almost
always cause breakage, crashes, or wasted time.

1.  **Use VS Code Tasks (run_task) for SWA lifecycle — NEVER run_in_terminal**
    -   Start: `run_task` with id **`shell: swa start`**
    -   Stop: `run_task` with id **`shell: swa stop`**
    -   Restart: `run_task` with id **`shell: swa restart`**
    -   ❌ **NEVER** run `swa start`, `swa stop`, `npx swa start`, or `swa cli` via `run_in_terminal`.
    -   The tasks handle Azurite lifecycle, terminal presentation, port cleanup, and process management.
    -   If tasks fail, diagnose the failure — do NOT fall back to `run_in_terminal`.
    -   **Why?** Running `swa start` directly bypasses Azurite cleanup and leads to port conflicts, zombie processes, and stale instances.
2.  **Never kill broad processes in the dev container**
    -   ❌ Do NOT use: `pkill -f`, `pkill -9`, `kill -9`, `killall`, `fuser -k`, or mass port killing.
    -   These can kill the **VS Code Server** (a Node.js process), disconnecting the container.
    -   ✅ Safe alternatives: use `run_task` → `shell: swa stop`, or identify exact PIDs with `ps aux | grep <name>` and `kill <pid>`.
3.  **Never start Azurite manually**
    -   SWA CLI starts Azurite automatically. Starting it manually causes port conflicts and crash loops.
4.  **Always restart SWA after ANY backend change**
    -   Any file under `/api/` → restart required. Skipping this means testing stale code.
5.  **Always test via `http://127.0.0.1:4280` before marking work complete**
    -   This ensures authentication + routing + proxy are validated.
6.  **Use `STORAGE` — never `AzureWebJobsStorage`**
    -   `AzureWebJobsStorage` is reserved by SWA.
7.  **Never use routes starting with `admin`**
    -   Azure Functions reserves the `/admin` prefix for internal endpoints.
    -   ❌ Avoid: `admin-seed-data`, `admin/users`, `adminTools`
    -   ✅ Use instead: `manage/seed-data`, `management/users`, `tools/admin`
8.  **Always use UTC for DateTime values**
    -   Azure Table Storage rejects non-UTC DateTime values.
    -   Use `DateTime.UtcNow` or `DateTime.SpecifyKind(date, DateTimeKind.Utc)`.

# 🏗 Project Architecture

-   **Frontend**: Vanilla JS, HTML, CSS — ❌ no React / Vue / etc.
-   **Backend**: .NET 8 Azure Functions (isolated worker)
-   **Storage**: Azure Table Storage (no SQL)
-   **AI**: Azure AI Document Intelligence (invoice extraction, mock in dev)
-   **Hosting**: Azure Static Web Apps
-   **Frontend Layout**: `/` = public pages, `/app/` = authenticated UI (protected routes)

# 🔁 Development Workflow

- **Frontend**: Edit → reload browser → test at `http://127.0.0.1:4280`. No restart needed.
- **Backend**: Save C# → `run_task` restart → test at `http://127.0.0.1:4280`. **Stale code if skipped.**

# 🛠 Troubleshooting

If ports seem blocked (especially Azurite on 10000–10002):

```bash
lsof -i :4280
lsof -i :7071
lsof -i :10000-10002
```

The **"swa stop"** task automatically cleans up Azurite processes.
If you need to manually kill specific PIDs: `kill <pid>`

# 🔐 Authentication
<!-- AUTH-SECTION-START -->

This project uses **Mock Authentication** for local development.

## Mock Auth Overview

Mock authentication provides a complete authentication simulation without
requiring Azure AD configuration. It includes:

- Email-based login flow
- Simulated MFA verification (use code: `123456`)
- Pre-configured demo personas with different roles
- Session management via `X-Session-Token` header

## Authentication Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/auth/mode` | GET | Returns current auth mode (`mock` or `swa`) |
| `/api/auth/me` | GET | Returns current user (requires session token) |
| `/api/auth/login` | POST | Initiates login with email |
| `/api/auth/mfa` | POST | Verifies MFA code (use `123456`) |
| `/api/auth/logout` | POST | Ends the session |

## Testing Authentication

1. Navigate to `http://127.0.0.1:4280/login.html`
2. Select a demo persona or enter an email
3. Enter MFA code: `123456`
4. You'll be redirected to `/app/` with full session

## Demo Users

Demo personas are loaded from `data/demo-users.csv`. Read the CSV for available users and roles.

## Session Token Usage

For API testing with authentication:

- Login: `curl -X POST 127.0.0.1:4280/api/auth/login -H "Content-Type: application/json" -d '{"email":"admin@demo.example.com"}'`
- MFA: `curl -X POST 127.0.0.1:4280/api/auth/mfa -H "Content-Type: application/json" -d '{"sessionToken":"<token>","code":"123456"}'`
- Me: `curl 127.0.0.1:4280/api/auth/me -H "X-Session-Token: <token>"`

## Frontend Auth Integration

**⚠️ IMPORTANT**: Every page script under `/app/` that calls a protected API **must** follow this pattern — no exceptions.

```javascript
// ✅ CORRECT — Standard pattern for any /app/ page script
async function init() {
    // 1. Wait for the app shell to confirm the user is authenticated.
    //    This prevents API calls firing before the session token is ready.
    //    Resolves immediately if auth is already done (no performance cost).
    await authService.waitForAppShell();

    // 2. Make authenticated API calls using fetchWithAuth().
    //    This automatically attaches the X-Session-Token header in mock mode
    //    and works transparently in SWA mode — no manual token handling needed.
    const response = await authService.fetchWithAuth('/api/my-endpoint');
    const data = await response.json();
}

// ❌ WRONG — Causes intermittent 401s and random redirects to login
const response = await fetch('/api/my-endpoint'); // Missing session token header!

// ❌ WRONG - Don't access localStorage directly
// localStorage.getItem('sessionToken')       // Wrong key name!
// localStorage.getItem('mock_session_token') // Breaks encapsulation
```

**Why `waitForAppShell()` is required**: `app.js` loads `authService` and resolves
the session token asynchronously. Page scripts that call APIs before this finishes
will send requests without the token, receive a 401, and redirect the user to login.
`waitForAppShell()` eliminates this race condition with zero boilerplate.

The session token is stored internally by `authService`. Direct localStorage
access will break when switching between mock and SWA authentication modes.

<!-- AUTH-SECTION-END -->

# 🧠 Backend Development Rules

-   Always use `AuthorizationLevel.Anonymous` for SWA-proxied APIs.
-   Always use camelCase JSON.
-   Always use `STORAGE` for all storage operations.

## ⚠️ CRITICAL: DateTime Must Be UTC

Azure Table Storage REJECTS non-UTC DateTime values at runtime.

```csharp
// ✅ CORRECT
entity.CreatedAt = DateTime.UtcNow;
entity.ExpiresAt = DateTime.SpecifyKind(someDate, DateTimeKind.Utc);

// ❌ WRONG - These throw System.NotSupportedException
entity.CreatedAt = DateTime.Now;              // Kind = Local
entity.CreatedAt = new DateTime(2026, 1, 29); // Kind = Unspecified
```

**Rule**: Before assigning ANY `DateTime` to an Azure Table entity, ensure `myDate.Kind == DateTimeKind.Utc`.

## ⚠️ CRITICAL: JSON Serialization via WorkerOptions

`Microsoft.AspNetCore.Http.Json.JsonOptions` does NOT affect Azure Functions Worker's `WriteAsJsonAsync` — this causes PascalCase JSON instead of camelCase, breaking frontend integration.

**Rule**: Do NOT modify the WorkerOptions serializer configuration in Program.cs. It is already correctly configured for camelCase output.

# 🎨 Frontend Development Rules

-   **Never use `alert()`, `confirm()`, or `prompt()`** — use modals instead (`showModal()`, `showConfirmModal()`).
    Native browser dialogs block the UI thread, cannot be styled, and provide a poor UX.

## ⚠️ CRITICAL: SWA Routing — Never Rely on Default Documents or JS Redirects

Azure Static Web Apps does **not** reliably serve `index.html` as a default
document for subdirectories (e.g., `/app/` → `app/index.html`). JavaScript-based
redirects inside `index.html` files will **fail with a 500** in production.

**Rule**: If a directory path like `/app/` should redirect to a specific page,
add an **explicit redirect route** in `staticwebapp.config.json`:

```json
{
  "route": "/app/",
  "redirect": "/app/dashboard.html",
  "statusCode": 302
}
```

-   Redirect routes must appear **before** wildcard routes (e.g., `/app/*`).
-   Apply the same redirect to all three config files: `staticwebapp.config.json`,
    `staticwebapp.config.mock.json`, `staticwebapp.config.swa.json`.
-   The SWA CLI emulator may mask this issue locally — always validate routing
    assumptions against [SWA routing docs](https://learn.microsoft.com/en-us/azure/static-web-apps/configuration).

# 🌱 Sample Data & Demo Users

## Seed Data Requirements

-   **Always implement seed/sample data** for new features that require data.
-   Seed data must be **triggered manually** (not auto-seeded on startup).
-   Provide a clear mechanism to invoke seeding (e.g., an API endpoint or admin function).
-   Seed data should be idempotent — running it multiple times should not create duplicates.

## Demo Users

See `data/demo-users.csv` for schema and existing personas. Add rows following the same format.
When adding new features that require specific user types, add corresponding demo users to this file.

# 🧪 Testing

- Primary test URL: `http://127.0.0.1:4280`
- Direct API debugging: `http://127.0.0.1:7071/api/...`

# ⚙ Configuration Contract

Never commit: secrets, connection strings, `.azurite/`, `tools/.azure-config`.

# 📁 Folder Exclusions

The `/tools/` folder contains helper scripts for Azure resource setup and configuration.
**This folder is NOT part of the solution** and should be ignored by the agent unless
specifically directed by the user.

# 🏁 Completion Criteria

-   UI tested via browser
-   API tested via SWA proxy
-   No console errors
-   Auth flow verified
-   SWA restarted after backend changes

<!-- AGENT-INSTRUCTIONS-MERGED -->
# Copilot Coding Agent --- GitHub Actions Environment Overrides

> **Purpose:** This file defines **environment-specific overrides** that
> apply **only** when the coding agent is running inside the **ephemeral
> GitHub Actions sandbox**.
>
> All other behaviour is governed by the **primary Copilot Coding Agent
> Instructions**.

------------------------------------------------------------------------

# ⚠️ Execution Environment Differences

The GitHub Actions sandbox **does not provide a full VS Code runtime**.

### Implications

-   `.vscode/tasks.json` **cannot be used**
-   VS Code task commands **will not execute**
-   All commands must be run **directly in the terminal**
-   Do not upgrade dependencies or modify lockfiles unless explicitly required for the task.

This **overrides the default instruction to use VS Code tasks**.

------------------------------------------------------------------------

# 🚀 SWA CLI Execution Overrides

These commands **replace** the VS Code task workflow defined in the
primary instructions.

All SWA lifecycle operations use a single script: `./scripts/swa-start.sh`

## Start SWA

``` bash
./scripts/swa-start.sh
```

This will: clean up stale processes → build .NET functions → start SWA in background → poll until all services (SWA proxy, Functions host, API routing) are confirmed ready (120s timeout).

If the script exits with code 0, SWA is fully operational at `http://127.0.0.1:4280`.

------------------------------------------------------------------------

## Stop SWA

``` bash
./scripts/swa-start.sh --stop
```

------------------------------------------------------------------------

## Restart SWA After Backend Changes

After modifying **any file under `/api/`**:

``` bash
./scripts/swa-start.sh --restart
```

This stops SWA, rebuilds the functions, starts SWA, and waits for readiness.

To skip the rebuild (e.g. config-only changes):

``` bash
./scripts/swa-start.sh --restart --skip-build
```

------------------------------------------------------------------------

# 🔐 Authentication Override
<!-- AUTH-SECTION-START -->

No special authentication handling required for GitHub Actions.
Mock authentication works identically in CI - see the primary instructions file for authentication details.

<!-- AUTH-SECTION-END -->

------------------------------------------------------------------------

# 🔧 Azure Functions Trigger Constraints

**CRITICAL**: Azure Static Web Apps with Managed Functions **only support HTTP triggers**.

## Rules

-   ❌ **NEVER** use: Timer triggers, Queue triggers, Blob triggers, Event Grid triggers, or any non-HTTP trigger
-   ✅ **ALWAYS** use: `[Function("FunctionName")]` with `[HttpTrigger]` only
-   All functions must use `AuthorizationLevel.Anonymous` (SWA handles auth at the proxy level)

## Background Operations

If you need timer-based or queue-based operations (e.g., scheduled jobs, async processing):

-   Implement as an **admin function** with an HTTP trigger
-   Protect with appropriate authorization checks
-   Expose via a **manual trigger** (button in UI)
-   Document that it requires manual invocation

**Example**:
```csharp
// ✅ CORRECT - HTTP trigger with manual admin invocation
[Function("ManageSeedData")]
public async Task<HttpResponseData> SeedData(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/seed-data")] 
    HttpRequestData req)
{
    // Check user has admin role
    // Perform background work
    // Return status
}
```

```csharp
// ❌ WRONG - Timer trigger not supported in SWA Managed Functions
[Function("ScheduledCleanup")]
public void Run([TimerTrigger("0 0 * * *")] TimerInfo timer)
{
    // This will NOT work in Azure Static Web Apps
}
```

------------------------------------------------------------------------

# 🧪 Testing Override

In GitHub Actions:

-   Always test via:

        http://127.0.0.1:4280

-   Use the authentication method described in the primary instructions.

------------------------------------------------------------------------

# 🏁 Completion Gate (CI)

Work is complete **only when**:

-   SWA is running
-   Authentication verified
-   `/app/` renders
-   APIs respond
-   No sample/template logic remains

------------------------------------------------------------------------

**This file only defines GitHub Actions execution overrides.**\
**All other behaviour is inherited from the primary instruction set.**
