```skill
---
name: enterprise-ui
description: Use when building UI for enterprise web applications. Enforces the design system from docs/DESIGN.md, ensures visual consistency across pages, and prevents ad-hoc styling. Always active during agentic builds that involve frontend work.
---

# Enterprise UI Skill

This skill ensures every page and component built by an AI coding agent follows the established design system. Enterprise applications need consistency — not creativity. Users trust interfaces that feel predictable and professional.

## Before Building Any UI

**MANDATORY**: Read `docs/DESIGN.md` completely before writing any HTML or CSS. This file contains:

- The exact `<head>` block every page must use
- Font families and how they map to Tailwind classes
- Color tokens and where each color is used
- Navigation structure and pattern
- Component HTML snippets with exact Tailwind classes
- Page layout patterns for each page type
- Spacing rhythm values

If `docs/DESIGN.md` does not exist, flag this as a 🔵 **Ambiguity** problem in AGENT_PROBLEMS.md and proceed using sensible defaults with Tailwind's default theme.

## Core Rules

### 1. Follow the Design System — Do Not Improvise

- Use ONLY the colors defined in the Tailwind config from DESIGN.md
- Use ONLY the font families specified (`font-heading`, `font-body`)
- Use ONLY the component patterns documented (Card, Button, Table, etc.)
- Copy the exact class strings from DESIGN.md — do not paraphrase them
- If a component you need isn't in DESIGN.md, build it using the same color tokens, spacing rhythm, and border-radius conventions as existing components

### 2. Reuse the Standard Head Block

Every HTML page MUST include the standard `<head>` block from DESIGN.md. This block includes:
- `<link rel="stylesheet" href="/vendor/fonts.css">` — vendored fonts
- `<script src="/vendor/tailwindcss.js"></script>` — vendored Tailwind CDN
- The inline `tailwind.config = { ... }` block with brand colors and fonts
- Viewport meta tag

Copy this block exactly. Do NOT modify the Tailwind config per-page.

### 3. Reuse the App Shell

Every authenticated page (`/app/*`) MUST reuse the same navigation and layout wrapper established in Issue 1. This means:
- Same sidebar or top nav on every page (same HTML structure, same links)
- Same footer on every page
- Same content wrapper with consistent padding
- The navigation highlights the active page

If building the first authenticated page, establish the app shell and document it in BUILD_LOG.md for future issues.

### 3a. Display the Client Logo

The client's logo MUST be displayed in the application. The logo file is stored in the repo at `docs/assets/logo.png` (or `.svg`/`.jpg` — check `docs/CLIENT.md` for the exact path).

**Required placements:**
- **Navigation**: The logo MUST appear as the first element in the sidebar header or the left side of the top navigation bar. It should link to the app's home/dashboard page. Use the exact HTML from DESIGN.md's Logo section.
- **Public landing page (`index.html`)**: The logo MUST appear in the page header or hero section.

**Rules:**
- Use the local file path (e.g., `/docs/assets/logo.png`) — do NOT use an external URL
- Always include an `alt` attribute with the company name
- Constrain height with Tailwind classes (e.g., `h-8` for top nav, `h-10` for sidebar) — do NOT distort aspect ratio
- Follow the exact placement and classes specified in DESIGN.md's Logo section

### 4. Never Use Raw Hex Colors

❌ **Wrong**: `bg-[#1B4D7A]`, `text-[#E8A530]`, `border-[#ddd]`
✅ **Correct**: `bg-primary`, `text-accent`, `border-gray-200`

All colors must come from the Tailwind theme config. This ensures a single source of truth for the brand palette.

### 5. Never Use External Resources

Due to firewall restrictions on the build runner:
- ❌ No Google Fonts links
- ❌ No external CDN links (Font Awesome, etc.)
- ❌ No `<link>` tags pointing to external URLs
- ✅ Use `/vendor/fonts.css` for fonts
- ✅ Use `/vendor/tailwindcss.js` for Tailwind
- ✅ Use inline SVG for icons

### 6. Responsive by Default

Every page must work at all standard breakpoints:
- `sm` (640px) — mobile phones
- `md` (768px) — tablets
- `lg` (1024px) — small laptops
- `xl` (1280px) — desktops

Use Tailwind's responsive prefixes (`sm:`, `md:`, `lg:`) rather than custom media queries.

### 7. Accessible

- Use semantic HTML: `<nav>`, `<main>`, `<header>`, `<footer>`, `<section>`
- All images need `alt` text
- All form inputs need associated `<label>` elements
- Interactive elements need visible focus states (Tailwind's `focus:ring-*`)
- Color alone must not convey meaning — pair with text or icons
- Use sufficient contrast ratios (WCAG AA minimum)

### 8. Consistent Empty and Loading States

Every page that loads async data must handle:
- **Loading**: Show the loading spinner from DESIGN.md
- **Empty**: Show the empty state pattern from DESIGN.md
- **Error**: Show the error toast/notification pattern from DESIGN.md

Do not leave pages blank while loading or show raw error messages.

## Common Mistakes to Avoid

| Mistake | Fix |
|---------|-----|
| Different navigation on different pages | Copy the nav HTML exactly from Issue 1's app shell |
| Missing client logo in navigation | Check DESIGN.md Logo section and `docs/CLIENT.md` for the logo path |
| Using external logo URL instead of local file | Use `/docs/assets/logo.png` (local path), not `https://img.logo.dev/...` |
| Ad-hoc card styles | Use the Card component from DESIGN.md |
| `text-blue-600` instead of `text-primary` | Use theme tokens, not Tailwind defaults |
| Missing hover/focus states on buttons | Use the Button patterns from DESIGN.md which include hover and focus |
| `style="..."` inline CSS | Use Tailwind classes instead |
| Importing Google Fonts | Fonts are vendored locally — use the `<head>` block from DESIGN.md |
| Different spacing on each page | Follow the spacing rhythm from DESIGN.md |
| Forgetting mobile layout | Use responsive Tailwind prefixes |

## Quality Checklist

Before submitting a PR that includes frontend changes:

- [ ] Standard `<head>` block from DESIGN.md is on every new HTML page
- [ ] Client logo displayed in navigation (sidebar header or top-left of nav bar)
- [ ] Client logo displayed on public landing page (index.html)
- [ ] Navigation is identical on every authenticated page
- [ ] Brand primary color is used for all buttons/CTAs
- [ ] No raw hex colors in HTML — all via Tailwind theme classes
- [ ] All new components follow patterns from DESIGN.md
- [ ] Empty states show helpful messages, not blank areas
- [ ] Loading states are shown for async operations
- [ ] Pages render correctly at 375px width (mobile)
- [ ] Pages render correctly at 1280px width (desktop)
- [ ] All form inputs have labels
- [ ] All interactive elements have visible focus states
```
