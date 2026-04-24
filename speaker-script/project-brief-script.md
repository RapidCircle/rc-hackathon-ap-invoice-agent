# AP Invoice Intake Agent — Speaker Script

> Use this script to explain the project to Team 1 (Sagarika, Ameya, Girish) during the hackathon kickoff.
> Estimated speaking time: 8-10 minutes.

---

## OPENING (1 minute)

> "Alright Team 1 — Sagarika, Ameya, Girish — your project is the **AP Invoice Intake Agent**. This is probably the most AI-heavy project in the hackathon, so let's walk through it."

> "In simple terms: you're building a system that **reads invoice PDFs using AI, pulls out all the important data, checks if it's correct, and sends it to the accounting system**. Think of it as an intelligent assistant for the finance team."

---

## THE PROBLEM (1 minute)

> "Here's the problem we're solving. Right now, when a company like ours receives invoices from vendors — TCS, Infosys, Wipro — someone in finance has to:"

> "**One** — Open the email, download the PDF."
> "**Two** — Read the invoice and manually type in the vendor name, invoice number, amount, date, tax, PO number into a spreadsheet."
> "**Three** — Check if the vendor is in our system, if the PO number is valid, if the amount makes sense."
> "**Four** — Enter all of this into Zoho Books, our accounting system."

> "For 50-100 invoices a month, that's hours of manual work. And humans make mistakes — wrong amounts, duplicate entries, missed invoices."

---

## THE SOLUTION (2 minutes)

> "Your app automates all of this. Here's the flow — think of it as a pipeline with 6 stages:"

> "**Stage 1 — INGEST:** A vendor sends an invoice PDF. The system receives it."

> "**Stage 2 — EXTRACT:** This is where the AI magic happens. **Azure AI Document Intelligence** — which is already provisioned and integrated for you — reads the PDF and extracts 8 fields: vendor name, invoice number, date, currency, total amount, tax amount, PO number, and line items. You don't need to train any model — Microsoft's prebuilt invoice model handles this."

> "**Stage 3 — VALIDATE:** The system runs 8 automatic checks. Is the vendor in our master list? Is the invoice number present? Is the currency a valid ISO code? Is the amount positive? Is it a duplicate? All 8 must pass."

> "**Stage 4 — ROUTE:** If all checks pass, the invoice is marked 'Ready for Zoho'. If any fail, it's flagged as an 'Exception'."

> "**Stage 5 — REVIEW:** A finance admin reviews the exceptions, corrects the data, and re-validates."

> "**Stage 6 — PUSH:** Validated invoices are batched and pushed to Zoho Books."

---

## WHAT'S ALREADY BUILT (2 minutes)

> "Now the good news — you're not starting from zero. We've built a complete working scaffold:"

> "**The frontend is done** — dashboard with stats, invoice list with search and create form, invoice detail page with an 8-step validation stepper, file upload with drag-and-drop and PDF preview, vendor management page. All styled with Tailwind."

> "**The backend is done** — .NET 8 Azure Functions with full CRUD APIs for invoices, vendors, batches. Validation service with all 8 checks. Demo data seeder with 10 real Indian IT company vendors and 20 invoices."

> "**The AI is provisioned** — Azure Document Intelligence is set up, the endpoint and API key are already in the environment variables. The extraction code is written in `ExtractionService.cs` — right now it returns mock data, but the real implementation is right there, commented out. You literally just uncomment it."

> "**The infrastructure is ready** — Azure Static Web App is deployed and live, GitHub Actions for deployment, Codespace devcontainer for development. Everything works."

---

## WHAT YOU NEED TO BUILD (2 minutes)

> "So what's left for you? Here are your priorities:"

> "**Priority 1 — the must-haves for the demo:**"
> "Switch the AI extraction from mock to real — uncomment the code in ExtractionService.cs, upload a real invoice PDF, and show it extracting fields live. That's your wow moment."
> "Store uploaded invoice files in Azure Blob Storage so you can preview them."
> "Make the end-to-end flow work: upload PDF → AI extracts → validation runs → see results."

> "**Priority 2 — nice to haves:**"
> "Let admins edit extracted fields inline."
> "Add charts to the dashboard."
> "Improve the vendor matching with fuzzy search."

> "**Stretch goals if you're flying:**"
> "Real Zoho Books integration. Audit trail. CSV export."

---

## HOW TO GET STARTED (1 minute)

> "Here's what you do right now:"

> "**Step 1** — Open the repo in GitHub Codespaces. Click Code → Codespaces → Create. Wait 3-5 minutes for the first build."

> "**Step 2** — In the terminal, run: `bash scripts/swa-start.sh` — this installs everything, builds the backend, and starts the app."

> "**Step 3** — Open port 4280. Seed test data. Sign in with MFA code 123456. Click around, explore the app."

> "**Step 4** — Read `PROJECT-BRIEF.md` — it has the full architecture, key files, and what each file does."

> "**Step 5** — Start coding! Use GitHub Copilot — it's configured with project-specific instructions. Ask it to uncomment the extraction service, add blob storage, whatever you need."

---

## KEY FILES TO REMEMBER (30 seconds)

> "Quick reference — the files you'll touch most:"

> "`api/Services/ExtractionService.cs` — this is your AI integration point. Uncomment the real code."
> "`api/Services/ValidationService.cs` — the 8 validation checks."
> "`app/js/invoice-detail.js` — the invoice detail page with the stepper and upload."
> "`api/local.settings.template.json` — your environment variables."

---

## CLOSING (30 seconds)

> "The live app is at gentle-grass-03ab54203.7.azurestaticapps.net — you can show it to people right now."

> "Your test invoices are in the `test-invoices/` folder — open them in browser, save as PDF, upload them."

> "Remember: deployments are manual. Push to main, then go to GitHub Actions and click Run Workflow."

> "Any questions? Great, let's build!"

---

## QUICK REFERENCE CARD

| Item | Value |
|------|-------|
| Live URL | https://gentle-grass-03ab54203.7.azurestaticapps.net |
| Repo | https://github.com/RapidCircle/rc-hackathon-ap-invoice-agent |
| Codespace start | `bash scripts/swa-start.sh` |
| MFA code | `123456` |
| AI endpoint | Already in env vars — no setup needed |
| Deploy | GitHub Actions → Run workflow (manual) |
| Team | Sagarika (AI), Ameya (Frontend), Girish (Backend) |
