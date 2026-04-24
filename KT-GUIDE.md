# Hackathon Knowledge Transfer (KT) Guide

> Quick-reference guide for all participants. Read before the hackathon or follow along during the KT sessions.

---

## KT 1: Git & GitHub Basics (For Non-Developers)

### What is Git?
Git is a **version control system** — it tracks every change to your code so you can go back in time, work in parallel, and merge everyone's work together.

Think of it like **Google Docs version history**, but for code.

### What is GitHub?
GitHub is where your Git repositories (projects) live online. It's like **OneDrive/SharePoint for code** — but with superpowers like pull requests, issues, and automated deployments.

### Key Concepts

| Concept | What It Means | Real-World Analogy |
|---------|---------------|-------------------|
| **Repository (Repo)** | A project folder tracked by Git | A SharePoint site for your project |
| **Clone** | Download a repo to your computer | "Sync" files from OneDrive to your laptop |
| **Branch** | A parallel copy to work on without affecting others | Making a copy of a Word doc to edit separately |
| **Commit** | Save a snapshot of your changes with a message | Clicking "Save" with a note about what you changed |
| **Push** | Upload your commits to GitHub | Syncing your local changes back to OneDrive |
| **Pull** | Download latest changes from GitHub | Refreshing to get others' latest changes |
| **Pull Request (PR)** | Ask to merge your branch into the main code | "Hey team, review my changes before we merge" |
| **Main branch** | The "live" version of the code | The published version of your document |
| **Merge conflict** | Two people changed the same line | Two people edited the same paragraph in Word |

### Commands You'll Use Today

```bash
# 1. Clone the repo (do this ONCE)
git clone https://github.com/YourTeam/your-repo.git
cd your-repo

# 2. Check what's changed
git status

# 3. Save your changes
git add .                        # Stage all changed files
git commit -m "Add search bar"   # Save with a message

# 4. Push to GitHub
git push

# 5. Pull latest from teammates
git pull

# 6. Create a branch (optional but recommended)
git checkout -b feature/add-charts    # Create and switch to new branch
# ... make changes ...
git add .
git commit -m "Add trend charts"
git push -u origin feature/add-charts # Push branch to GitHub
# Then create a Pull Request on GitHub website
```

### GitHub Website — What You Need to Know

| Tab | What It Does |
|-----|-------------|
| **Code** | Browse files, see the latest code |
| **Issues** | Track bugs and feature requests |
| **Pull Requests** | Review and merge code changes |
| **Actions** | CI/CD — automated build and deploy (runs when you push) |
| **Settings > Secrets** | Where deployment keys are stored (admin only) |

### Team Workflow for Today

```
1. Everyone clones the same repo
2. Option A (Simple): Everyone works on `main` branch
   - Pull before you start working: git pull
   - Commit and push frequently
   - Communicate who's editing which file to avoid conflicts

3. Option B (Better): Each person creates a branch
   - git checkout -b feature/my-feature
   - Work on your feature
   - Push and create a Pull Request
   - Someone reviews and merges

For a hackathon, Option A is fine if you communicate well!
```

### What If Something Goes Wrong?

| Problem | Fix |
|---------|-----|
| "I can't push — rejected" | Run `git pull` first, then `git push` |
| "Merge conflict" | Open the file, look for `<<<<<<` markers, pick the right version, save, `git add .`, `git commit -m "resolve conflict"` |
| "I broke everything" | `git stash` (saves your changes aside), `git pull` (get clean version), `git stash pop` (re-apply your changes) |
| "I want to undo my last commit" | `git reset --soft HEAD~1` (undo commit, keep changes) |
| "I want to start fresh" | Re-clone the repo |

---

## KT 2: Free AI Coding Tools — What They Are & How to Use Them

### Overview: What is "Vibe Coding"?

Vibe coding = **describing what you want in natural language and letting AI write the code for you**. You guide the direction, AI does the heavy lifting. You review, adjust, and iterate.

### Tool Categories

#### Category 1: AI-Powered Code Editors (Recommended for Hackathon)

These replace or enhance your code editor with AI built in:

---

#### Cursor (Recommended)
- **What:** Full IDE (based on VS Code) with AI built in
- **Free tier:** 50 premium requests/month (enough for hackathon)
- **Website:** https://cursor.com
- **Best for:** Writing new features, refactoring, debugging

**How to use Cursor:**
```
1. Download and install Cursor
2. Open your cloned repo folder in Cursor
3. Use these features:

   CMD/CTRL + K (in a file):
   → Highlight code → "Convert this to a responsive table"
   → Place cursor → "Add a search filter function here"

   CMD/CTRL + L (Chat panel):
   → "How does the authentication work in this project?"
   → "Write a function to export this table to CSV"
   → "Add a Chart.js bar chart showing weekly trends"

   Tab (Autocomplete):
   → Start typing and Cursor suggests the next lines
   → Press Tab to accept

   CMD/CTRL + I (Composer - Multi-file edits):
   → "Add a dark mode toggle to the nav bar and update all pages"
   → This can edit multiple files at once!
```

**Pro Tips for Cursor:**
- Use `@file` to reference specific files: "Look at @pipelineDashboard.js and add filters"
- Use `@codebase` to let it search the whole project
- Be specific: "Add a Chart.js line chart showing netChange values for the last 4 weeks" > "Add a chart"

---

#### GitHub Copilot (Free)
- **What:** AI pair programmer inside VS Code
- **Free tier:** Free for individual developers (2000 completions + 50 chat/month)
- **Website:** https://github.com/features/copilot
- **Best for:** Autocomplete, inline suggestions, quick fixes

**How to use Copilot:**
```
1. Install VS Code
2. Install "GitHub Copilot" extension from marketplace
3. Sign in with your GitHub account

   Inline suggestions:
   → Just start typing or write a comment describing what you want
   → Example: // function to format currency in euros
   → Copilot suggests the full function → Press Tab to accept

   Copilot Chat (sidebar):
   → Click the Copilot icon in sidebar
   → "Explain this function"
   → "Write a unit test for this"
   → "Fix the bug in this code"

   Inline Chat (CMD/CTRL + I):
   → Select code → "Make this responsive"
   → "Add error handling here"
```

---

#### Windsurf (by Codeium)
- **What:** AI-native IDE with "Cascade" agent mode
- **Free tier:** Generous free tier with AI completions
- **Website:** https://codeium.com/windsurf
- **Best for:** Agent-mode multi-file editing (similar to Cursor Composer)

**How to use Windsurf:**
```
1. Download Windsurf from codeium.com/windsurf
2. Open your repo folder
3. Use Cascade (AI agent):
   → "Add a dashboard page with summary statistics cards"
   → It will create/edit multiple files, add routes, update navigation
   → Review the changes before accepting
```

---

#### Trae IDE
- **What:** Free AI-native IDE by ByteDance
- **Free tier:** Completely free
- **Website:** https://trae.ai
- **Best for:** Full agent mode, Builder mode for multi-file changes

---

### Category 2: AI Chat Tools (Use Alongside Your Editor)

These are browser-based — use them to generate code snippets, then paste into your editor:

| Tool | Free Tier | Best Use | Link |
|------|-----------|----------|------|
| **ChatGPT** | GPT-4o free | "Write me a function that..." | https://chat.openai.com |
| **Claude** | Free tier | Complex logic, long code generation | https://claude.ai |
| **Google Gemini** | Free | Large context, paste whole files | https://gemini.google.com |

**How to use AI Chat effectively:**
```
Good prompt:
"I have a vanilla JavaScript app using Tailwind CSS. Write a function that
takes an array of opportunity objects with fields {client, title, status,
priority} and renders them as a filterable HTML table with search input.
Use the existing design system: bg-neutral-100 for headers, text-neutral-700
for body text, hover:bg-gray-50 for rows."

Bad prompt:
"Make a table"
```

**Power technique — paste your existing code:**
```
"Here is my current dashboard.js file: [paste file]

Add a new section that shows a pie chart of opportunities by status
using Chart.js CDN. Match the existing code style and design tokens."
```

---

### Category 3: AI App Builders (For Quick UI Generation)

| Tool | What It Does | Free Tier | Link |
|------|-------------|-----------|------|
| **v0 by Vercel** | Generate React/HTML UI components from descriptions | Free | https://v0.dev |
| **Bolt.new** | Generate full-stack apps in browser | Limited free | https://bolt.new |
| **Lovable** | Generate apps from descriptions | Limited free | https://lovable.dev |
| **Firebase Studio** | Google's AI app builder | Free | https://firebase.google.com/studio |

**How to use v0 for this hackathon:**
```
1. Go to v0.dev
2. Describe what you want:
   "Create a dashboard card component with:
   - Title 'Pipeline Summary'
   - Three stat boxes showing Starting Value, Ending Value, Net Change
   - Net Change should be green if positive, red if negative
   - Use a clean professional design with blue (#0050A0) as primary color"
3. Copy the generated HTML/CSS
4. Paste into your project and adapt to match the existing design system
```

---

### Which Tool Should I Pick?

| Your Skill Level | Recommended Tool | Why |
|-----------------|------------------|-----|
| **Never coded before** | Cursor + ChatGPT | Cursor handles files, ChatGPT explains things |
| **Some coding experience** | Cursor or GitHub Copilot | Best productivity boost |
| **Experienced developer** | GitHub Copilot + Claude | Fast autocomplete + complex problem solving |
| **Want to experiment** | Bolt.new or v0 | Generate UI fast, copy into project |

---

## KT 3: What is a Web Application? (Architecture Basics)

### The Big Picture

```
┌─────────────────────────────────────────────────────────┐
│                     YOUR BROWSER                         │
│                                                          │
│   ┌──────────────────────────────────────────────┐      │
│   │              FRONTEND                         │      │
│   │   HTML  → Structure (skeleton)                │      │
│   │   CSS   → Styling (how it looks)              │      │
│   │   JS    → Behavior (what happens on click)    │      │
│   └──────────────────┬───────────────────────────┘      │
│                      │ HTTP Request (fetch/API call)     │
└──────────────────────┼──────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────┐
│                     BACKEND (Server)                      │
│                                                           │
│   Azure Functions (.NET C#)                               │
│   ┌─────────────────────────────────────────────┐        │
│   │  /api/opportunities  → Returns JSON data     │        │
│   │  /api/pipeline/weekly-report → Returns JSON  │        │
│   │  /api/auth/login     → Handles login         │        │
│   └──────────────────┬──────────────────────────┘        │
│                      │                                    │
│                      ▼                                    │
│   ┌─────────────────────────────────────────────┐        │
│   │           DATABASE                           │        │
│   │   Azure Table Storage                        │        │
│   │   (Stores opportunities, users, etc.)        │        │
│   └─────────────────────────────────────────────┘        │
└──────────────────────────────────────────────────────────┘
```

### How Our Apps Work

| Layer | Technology | What It Does | Files |
|-------|-----------|-------------|-------|
| **Frontend** | HTML + Tailwind CSS + Vanilla JS | What users see and interact with | `index.html`, `app/*.html`, `app/js/*.js` |
| **Backend API** | .NET 8 Azure Functions | Processes requests, business logic | `api/Functions/*.cs`, `api/Services/*.cs` |
| **Database** | Azure Table Storage (Azurite locally) | Stores all data | `api/Models/*.cs` (defines data shape) |
| **Auth** | Mock Auth (dev) / Azure AD (prod) | Who is logged in? | `js/authService.js`, `api/Auth/*.cs` |
| **Hosting** | Azure Static Web Apps | Serves the app on the internet | `staticwebapp.config.json` |

### How a User Action Flows Through the App

```
User clicks "Apply" on week selector
         │
         ▼
Browser JS (pipelineDashboard.js)
  → fetchWeeklyReport("2026-02-02")
  → fetch("/api/pipeline/weekly-report?weekStart=2026-02-02")
         │
         ▼ HTTP GET request
Backend Azure Function (WeeklyPipelineReportFunctions.cs)
  → Reads weekStart from query string
  → Calls PipelineReportService.GetWeeklyReportAsync()
         │
         ▼
Service Layer (PipelineReportService.cs)
  → Queries Azure Table Storage for snapshots
  → Queries movements for each opportunity type
  → Calculates totals
  → Returns WeeklyPipelineSummaryDto
         │
         ▼ JSON response
Browser JS receives JSON
  → renderSummary(data) — updates the dashboard cards
  → renderMovementBreakdown(data.typeSummaries) — builds the detail tables
         │
         ▼
User sees the updated dashboard!
```

### Key Folders Explained

```
your-repo/
├── index.html              ← Landing page (what you see first)
├── login.html              ← Login page
├── app/                    ← Protected pages (need login)
│   ├── dashboard.html      ← Main dashboard
│   ├── js/                 ← JavaScript for each page
│   │   ├── dashboard.js
│   │   └── ...
├── api/                    ← Backend code (.NET C#)
│   ├── Functions/          ← API endpoints (HTTP triggers)
│   ├── Services/           ← Business logic
│   ├── Models/             ← Data structures
│   └── Program.cs          ← App startup / dependency injection
├── js/
│   └── authService.js      ← Authentication helper (used by all pages)
├── docs/                   ← Documentation
│   ├── SPECIFICATION.md    ← What to build
│   └── DESIGN.md           ← How it should look
├── vendor/                 ← Third-party libraries (Tailwind CSS, fonts)
├── tools/                  ← Setup and deployment scripts
└── scripts/
    └── swa-start.sh        ← Start the local dev server
```

### "Where Do I Add My Feature?"

| I Want To... | Edit This | Example |
|-------------|----------|---------|
| Change how the page looks | `.html` files | Add a new section to `dashboard.html` |
| Add a new button click behavior | `app/js/*.js` files | Add event listener in `dashboard.js` |
| Change styling | Use Tailwind classes in HTML | `class="bg-primary text-white rounded-lg"` |
| Add a new API endpoint | Create new `.cs` in `api/Functions/` | Copy pattern from existing function files |
| Add new business logic | Create/edit `.cs` in `api/Services/` | Add new method to existing service |
| Store new data | Create `.cs` in `api/Models/` | Define new entity class |
| Add a chart | Add Chart.js CDN to HTML + write JS | `<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>` |

---

## KT 4: How to Deploy (Local → Azure Cloud)

### Step 1: Local Development (Where You Are Now)

```
Your Laptop
┌──────────────────────────────────────┐
│  SWA CLI (port 4280) ← You open this│
│    │                                  │
│    ├── Serves HTML/JS/CSS files       │
│    │                                  │
│    └── Proxies /api/* requests to:    │
│        Azure Functions (port 7071)    │
│          │                            │
│          └── Reads/Writes data to:    │
│              Azurite (port 10002)     │
│              (fake Azure Storage)     │
└──────────────────────────────────────┘
```

**You access it at:** `http://127.0.0.1:4280`

### Step 2: Push Code to GitHub

```bash
git add .
git commit -m "Add trend charts and search filter"
git push
```

### Step 3: Automatic Deployment (CI/CD)

When you push to `main` branch, **GitHub Actions automatically deploys** to Azure:

```
You push to GitHub
      │
      ▼
GitHub Actions workflow runs
(.github/workflows/azure-static-web-apps.yml)
      │
      ├── Step 1: Checkout code
      ├── Step 2: Build .NET backend (dotnet build)
      ├── Step 3: Package frontend (HTML/JS/CSS)
      ├── Step 4: Deploy to Azure Static Web Apps
      │
      ▼
Your app is LIVE at:
https://your-team-name.azurestaticapps.net
```

**You don't need to do anything special** — just push to `main` and wait 2-3 minutes!

### Step 4: Verify Deployment

```bash
# Check GitHub Actions status
# Go to your repo → Actions tab → See if the build is green ✓

# Or use command line
gh run list --limit 5
```

### What Happens in Azure (Production)

```
Internet Users
      │
      ▼
Azure Static Web Apps (CDN)
┌──────────────────────────────────────┐
│  Serves HTML/JS/CSS globally (fast)  │
│    │                                  │
│    └── Routes /api/* to:              │
│        Managed Azure Functions        │
│          │                            │
│          └── Reads/Writes to:         │
│              Azure Table Storage      │
│              (real cloud database)    │
│                                       │
│  Auth: Azure AD / Entra ID           │
│  (real Microsoft login)              │
└──────────────────────────────────────┘
```

### Local vs Deployed — What Changes?

| Aspect | Local (localhost) | Deployed (Azure) |
|--------|-------------------|-------------------|
| URL | `http://127.0.0.1:4280` | `https://your-app.azurestaticapps.net` |
| Database | Azurite (fake, on your laptop) | Azure Table Storage (real, in cloud) |
| Auth | Mock auth (code: 123456) | Azure AD (real Microsoft login) |
| Data | Seeded demo data | Empty (need to seed or connect CRM) |
| Speed | Local speed | Global CDN (fast worldwide) |
| Who can access | Only you | Anyone with the URL + login |

### Deployment Troubleshooting

| Problem | Check This |
|---------|-----------|
| Build failed (red X in Actions) | Click the failed run → Read the error log → Usually a C# compile error |
| Deploy succeeded but app shows blank | Check browser console (F12) for JS errors |
| API returns 500 errors on deployed version | Check Azure Portal → Static Web App → Monitor → Application Insights |
| "401 Unauthorized" on deployed app | Auth config issue — check `staticwebapp.config.json` routes |

---

## KT 5: Tailwind CSS — Quick Reference

### What is Tailwind?

Tailwind CSS = **utility-first CSS framework**. Instead of writing CSS files, you add classes directly to HTML:

```html
<!-- Without Tailwind (traditional CSS) -->
<div class="my-card">Hello</div>
<style>
.my-card {
    background: white;
    padding: 24px;
    border-radius: 8px;
    border: 1px solid #E5E7EB;
    box-shadow: 0 1px 2px rgba(0,0,0,0.05);
}
</style>

<!-- With Tailwind (what our app uses) -->
<div class="bg-white p-6 rounded-lg border border-gray-200 shadow-sm">Hello</div>
```

### Most Used Classes in Our App

| Category | Class | What It Does |
|----------|-------|-------------|
| **Background** | `bg-white` | White background |
| | `bg-gray-50` | Light gray (page background) |
| | `bg-primary` | Brand blue (#0050A0) |
| **Text** | `text-gray-700` | Body text color |
| | `text-gray-900` | Heading text color |
| | `text-sm` | Small text (14px) |
| | `text-lg` | Large text (18px) |
| | `font-bold` | Bold text |
| | `font-medium` | Medium weight |
| **Spacing** | `p-6` | Padding all sides (24px) |
| | `px-4` | Padding left+right (16px) |
| | `py-2` | Padding top+bottom (8px) |
| | `mt-4` | Margin top (16px) |
| | `mb-8` | Margin bottom (32px) |
| | `gap-4` | Gap between flex/grid items |
| **Layout** | `flex` | Flexbox container |
| | `flex-col` | Stack vertically |
| | `items-center` | Center vertically |
| | `justify-between` | Space items apart |
| | `grid grid-cols-3` | 3-column grid |
| **Border** | `border` | 1px border |
| | `border-gray-200` | Light gray border |
| | `rounded-lg` | Rounded corners (8px) |
| **Responsive** | `md:grid-cols-3` | 3 columns on medium+ screens |
| | `hidden md:flex` | Hidden on mobile, flex on desktop |
| **Interactive** | `hover:bg-gray-50` | Background on hover |
| | `cursor-pointer` | Pointer cursor |

### Quick Copy-Paste Components

**Card:**
```html
<div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
  <h3 class="text-sm font-semibold text-gray-900">Title</h3>
  <p class="text-gray-500 mt-1 text-sm">Description</p>
</div>
```

**Button (Primary):**
```html
<button class="bg-primary text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-primary-dark">
  Click Me
</button>
```

**Status Badge:**
```html
<span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-50 text-green-700">
  Active
</span>
```

**Search Input:**
```html
<input type="text" placeholder="Search..."
  class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50" />
```

---

## KT 6: Adding Common Features (Quick Recipes)

### Recipe 1: Add a Search/Filter to a Table

```javascript
// Add this to your page's JS file
function setupSearch(inputId, tableContainerId) {
    const input = document.getElementById(inputId);
    if (!input) return;

    input.addEventListener('input', function() {
        const query = this.value.toLowerCase();
        const rows = document.querySelectorAll('#' + tableContainerId + ' tbody tr');

        rows.forEach(function(row) {
            const text = row.textContent.toLowerCase();
            row.style.display = text.includes(query) ? '' : 'none';
        });
    });
}
```

```html
<!-- Add before your table -->
<input id="search-input" type="text" placeholder="Search opportunities..."
    class="w-full max-w-md border border-gray-300 rounded-lg px-3 py-2 text-sm mb-4
           focus:outline-none focus:ring-2 focus:ring-primary/50" />
```

### Recipe 2: Add a Chart (Chart.js)

```html
<!-- Add to your HTML <head> or before closing </body> -->
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>

<!-- Add where you want the chart -->
<div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
    <h3 class="text-sm font-semibold text-gray-900 mb-4">Pipeline Trend</h3>
    <canvas id="trend-chart" height="200"></canvas>
</div>
```

```javascript
// Add to your JS file
function renderTrendChart(labels, data) {
    const ctx = document.getElementById('trend-chart').getContext('2d');
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,   // ['Week 1', 'Week 2', 'Week 3']
            datasets: [{
                label: 'Net Change (€)',
                data: data,    // [65000, -5000, 30000]
                backgroundColor: data.map(v => v >= 0 ? '#16A34A' : '#DC2626')
            }]
        },
        options: {
            responsive: true,
            plugins: { legend: { display: false } }
        }
    });
}
```

### Recipe 3: Add CSV/Excel Export

```javascript
// Add to your JS file
function exportToCSV(data, filename) {
    if (!data || data.length === 0) return;

    const headers = Object.keys(data[0]);
    const csvRows = [headers.join(',')];

    data.forEach(function(row) {
        const values = headers.map(function(h) {
            var val = row[h] == null ? '' : String(row[h]);
            // Escape commas and quotes
            if (val.includes(',') || val.includes('"')) {
                val = '"' + val.replace(/"/g, '""') + '"';
            }
            return val;
        });
        csvRows.push(values.join(','));
    });

    const blob = new Blob([csvRows.join('\n')], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || 'export.csv';
    a.click();
    URL.revokeObjectURL(url);
}
```

```html
<!-- Add an export button -->
<button onclick="exportToCSV(opportunities, 'opportunities.csv')"
    class="border border-gray-300 text-gray-700 px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-50">
    Export CSV
</button>
```

### Recipe 4: Add Dark Mode Toggle

```html
<!-- Add to nav bar -->
<button id="dark-toggle" class="text-sm text-gray-600 hover:text-gray-900" onclick="toggleDark()">
    🌙 Dark
</button>
```

```javascript
function toggleDark() {
    document.documentElement.classList.toggle('dark');
    var btn = document.getElementById('dark-toggle');
    if (document.documentElement.classList.contains('dark')) {
        btn.textContent = '☀️ Light';
    } else {
        btn.textContent = '🌙 Dark';
    }
}
```

### Recipe 5: Add a New API Endpoint (Backend)

Create a new file `api/Functions/MyNewFunction.cs`:

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Api.Functions;

public class MyNewFunctions
{
    [Function("MyEndpoint")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "my-endpoint")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { message = "Hello from my new endpoint!" });
        return response;
    }
}
```

Then call it from JavaScript:
```javascript
const response = await authService.fetchWithAuth('/api/my-endpoint');
const data = await response.json();
console.log(data.message); // "Hello from my new endpoint!"
```

---

## Quick Reference Card (Print This!)

```
╔══════════════════════════════════════════════════════════╗
║              HACKATHON QUICK REFERENCE                   ║
╠══════════════════════════════════════════════════════════╣
║                                                          ║
║  START LOCAL SERVER:                                     ║
║    bash scripts/swa-start.sh                             ║
║    Open: http://127.0.0.1:4280                           ║
║    Login MFA code: 123456                                ║
║                                                          ║
║  GIT COMMANDS:                                           ║
║    git pull                  (get latest)                ║
║    git add .                 (stage changes)             ║
║    git commit -m "message"   (save snapshot)             ║
║    git push                  (upload to GitHub)          ║
║                                                          ║
║  KEY FILES:                                              ║
║    Frontend pages:  app/*.html                           ║
║    Frontend logic:  app/js/*.js                          ║
║    Backend APIs:    api/Functions/*.cs                    ║
║    Backend logic:   api/Services/*.cs                    ║
║    Data models:     api/Models/*.cs                      ║
║    Design system:   docs/DESIGN.md                       ║
║    Requirements:    docs/SPECIFICATION.md                ║
║                                                          ║
║  AI TOOLS:                                               ║
║    Cursor:     CMD+K (edit) | CMD+L (chat)               ║
║    Copilot:    Tab (accept) | CMD+I (inline chat)        ║
║    ChatGPT:    Paste code + describe what you want       ║
║                                                          ║
║  DEPLOY:                                                 ║
║    Just push to main → GitHub Actions auto-deploys       ║
║                                                          ║
║  HELP:                                                   ║
║    Ask your team → Ask AI → Ask organizer                ║
║                                                          ║
╚══════════════════════════════════════════════════════════╝
```
