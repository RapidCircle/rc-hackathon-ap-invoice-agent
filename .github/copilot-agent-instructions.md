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
