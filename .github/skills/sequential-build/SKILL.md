```skill
---
name: sequential-build
description: Use when working on a GitHub issue that is part of a multi-issue delivery plan, sequential build, MVP build, or when an issue mentions "depends_on", references previous issues, or is numbered (e.g., "Issue 3 of 8"). Provides context continuity between independent agent sessions working on related issues.
---

# Sequential Build Skill

This skill ensures continuity when an AI coding agent works on sequential GitHub issues that are part of a larger delivery plan. Each agent session is stateless, so this skill establishes patterns for context handover.

## Before Starting Work

**MANDATORY**: Before implementing anything, perform these steps:

### 1. Read the Build Log

Check if `docs/BUILD_LOG.md` exists and read it completely:

```
docs/BUILD_LOG.md
```

This file contains:
- Summary of what was built in previous issues
- Files created/modified in each issue
- Key implementation decisions and patterns established
- Any deviations from the original plan

### 2. Review Referenced Files

If the issue mentions `depends_on` or lists files from previous issues:
- Read those files to understand established patterns
- Note naming conventions, error handling approaches, and code structure
- Follow the same patterns for consistency

### 3. Check Key Documents

Always read these if they exist:
- `docs/SPECIFICATION.md` - What to build
- `docs/CLIENT.md` - Branding and company context
- `docs/AGENT_PROBLEMS.md` - Problems from previous issues (avoid repeating them)
- `.github/copilot-instructions.md` - Coding standards

## During Implementation

### Follow Established Patterns

When the codebase already has similar components:
- **Services**: Follow the existing `{Entity}Service.cs` pattern
- **Functions/Endpoints**: Follow the existing `{Entity}Functions.cs` pattern
- **Models**: Follow existing entity structure (PartitionKey/RowKey patterns for Table Storage)
- **Frontend**: Follow existing HTML structure, CSS classes, and JS patterns
- **Error handling**: Use the same error response shapes as existing code

Do NOT invent new patterns if existing code demonstrates a convention.

### Track Your Changes

Keep a mental note of:
- New files you create
- Key implementation decisions you make
- Any deviations from the plan (and why)
- Patterns you establish that future issues should follow
- **Problems encountered** - anything unexpected OR that required a workaround: code issues, build failures, deploy errors, environment problems (SWA not starting, Azurite issues, port conflicts), runner issues, browser automation failures, services not responding, retries needed (for `docs/AGENT_PROBLEMS.md`). **The bar is LOW** — if you had to retry, troubleshoot, or work around something, log it. A human reads these to fix recurring issues, so **be specific**: what happened, what you expected, and what you did to work around it.
- **What features were completed** — for the client-facing update (`docs/CLIENT_UPDATE.md`)

### Take Screenshots (When UI Changes)

If your issue includes **visible UI changes** (new pages, redesigned layouts, new forms, significant styling), take a screenshot to document progress:

1. Use the browser MCP to navigate to the most representative page you changed
2. **Resize the browser viewport to exactly 1280×720 pixels** using the browser MCP's `resize_page` tool — do this before EVERY screenshot to ensure consistent dimensions
3. Take the screenshot (it will now be exactly 1280×720)
4. Save it to `docs/screenshots/NN-short-description.png` where:
   - `NN` is the zero-padded issue number (e.g., `01`, `02`, `03`)
   - `short-description` summarizes what's shown (e.g., `dashboard`, `settings-form`, `user-list`)
4. Examples: `01-homepage.png`, `03-dashboard.png`, `05-settings-form.png`

**Skip screenshots** for:
- Backend-only changes (API endpoints, services, models)
- Configuration or infrastructure changes
- Refactoring with no visible UI difference
- Bug fixes that don't change the visual appearance

These screenshots are shown to the client as a visual progress carousel — each one should showcase meaningful progress.

## After Completing Work

**MANDATORY**: Before marking the issue complete, update the build log and client update.

### Update BUILD_LOG.md

Add a new section to `docs/BUILD_LOG.md` with:

```markdown
---

## Issue #[N]: [Issue Title]
**Status**: ✅ Completed  
**Date**: [Current Date]

### Files Created
- `path/to/new/file1.cs` - Brief description
- `path/to/new/file2.html` - Brief description

### Files Modified
- `path/to/existing/file.cs` - What was changed

### Key Implementation Decisions
- Decision 1: Brief explanation and rationale
- Decision 2: Brief explanation and rationale

### Patterns Established
- Pattern name: How to use it (if this is new to the codebase)

### Notes for Next Issues
- Any context the next agent session should know
- Gotchas or edge cases discovered
- Dependencies that were created
```

### If BUILD_LOG.md Doesn't Exist

Create it with this template:

```markdown
# Build Log

This document tracks implementation progress across sequential issues in a multi-issue delivery plan. It provides context for each agent session working on subsequent issues.

**How to use this file:**
- Read this BEFORE starting work on any issue
- Update this AFTER completing each issue
- Focus on decisions and patterns, not just file lists

---

## Issue #1: [Issue Title]
**Status**: ✅ Completed  
**Date**: [Date]

### Files Created
- [List files]

### Key Implementation Decisions
- [List decisions]

### Patterns Established
- [List patterns that future issues should follow]

### Notes for Next Issues
- [Any important context]
```

### Update CLIENT_UPDATE.md

**Append** a new update section to the end of `docs/CLIENT_UPDATE.md`. Each update is a self-contained progress snapshot for the issue you just completed. The client sees a list of updates and can browse through them, so each section must stand on its own.

This file is shown to the person who requested the application, so it must be:

- **Plain English** — no technical jargon (no file paths, class names, API references)
- **Feature-focused** — describe what the app *does*, not how it's coded
- **Honest** — clearly distinguish completed features from upcoming ones
- **Concise** — use bullet points and short paragraphs
- **No dates** — do not include any date headings, metadata lines, or timestamps

**IMPORTANT**: Do NOT overwrite previous updates. Append your new section after the last one.

Append this structure to the end of the file:

```markdown

---

## [Task Name]

### What's Been Done

[2-3 sentences describing what was accomplished in plain English]

### What's Next

[1-2 sentences describing what comes next]
```

**IMPORTANT**: `[Task Name]` must be the actual descriptive name of the task (e.g. "User Authentication", "Dashboard Layout", "Non-Project Capacity Blocks"), NOT generic labels like "Progress Update" or "Issue #N Complete". This name appears in the client's update navigator dropdown.

If `docs/CLIENT_UPDATE.md` doesn't exist yet, create it with a top-level heading and your first update:

```markdown
# Client Update

---

## [Task Name]

### What's Been Done

[2-3 sentences describing what was accomplished in plain English]

### What's Next

[1-2 sentences describing what comes next]
```

**Rules:**
1. Each section MUST start with `---` (horizontal rule) followed by `## [Task Name]`
2. The task name becomes the label in the client's update navigator — use the actual descriptive name of the task (e.g. "User Authentication", "Dashboard Layout"), NOT generic labels like "Progress Update" or "Issue #N"
3. Do NOT include dates, timestamps, or "Update 1/2/3" numbering
4. Do NOT repeat the full project history in each section — just describe what THIS issue accomplished
5. Keep each update brief — a short paragraph for "What's Been Done" and one for "What's Next"

## Quality Checklist

Before submitting your PR, verify:

- [ ] BUILD_LOG.md has been updated with your work
- [ ] CLIENT_UPDATE.md has been updated with a client-friendly progress report
- [ ] AGENT_PROBLEMS.md has ONLY problems logged (no implementation notes). Clean run = single line.
- [ ] Any workaround, retry, or troubleshooting step you performed is logged as a problem (even if you fixed it yourself)
- [ ] Screenshot saved to `docs/screenshots/` if this issue included UI changes
- [ ] You followed patterns from previous issues (check the log)
- [ ] New patterns are documented for future issues
- [ ] No conflicting approaches were introduced

## Why This Matters

Each agent session starts fresh with no memory of previous sessions. Without explicit handover:
- Patterns become inconsistent across issues
- Agents may redo work or conflict with previous decisions
- Context about "why" something was done is lost
- Integration issues arise when merging sequential PRs

The BUILD_LOG.md is the shared memory between agent sessions.
```
