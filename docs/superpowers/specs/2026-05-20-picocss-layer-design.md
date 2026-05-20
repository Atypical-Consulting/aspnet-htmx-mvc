# PicoCSS-flavored Class Layer — Design

**Date:** 2026-05-20
**Stack:** Tailwind Play CDN, ASP.NET Core MVC views (existing). No new packages, no build step.

## Goal

Extract repeated Tailwind utility class strings out of the views into a small set of element selectors and component classes. Each `.cshtml` should read like clean semantic HTML — plain `<button>`, `<input>`, `<table>`, with classes only where structurally needed.

## Non-goals

- A full design system (color tokens, spacing scale, typography ramp)
- CSS variables / theming
- Replacing Tailwind (the layout/utility usage stays)
- Touching `<header>` / `<main>` containers in `_Layout.cshtml`
- Visual regression tests
- Mobile / responsive tuning beyond what's already there
- New views, new pages, new components

## Where the CSS lives

One new `<style type="text/tailwindcss">` block inside `<head>` of `src/HtmxMvc.Web/Views/Shared/_Layout.cshtml`, immediately after the `<script src="https://cdn.tailwindcss.com">` line. Tailwind Play CDN processes the block as Tailwind input so `@apply` and `@layer` work.

```cshtml
<script src="https://cdn.tailwindcss.com"></script>
<style type="text/tailwindcss">
  @layer base   { /* element styling */ }
  @layer components { /* variant + helper classes */ }
</style>
```

## `@layer base` — element styling

```css
button {
  @apply bg-blue-600 hover:bg-blue-700 text-white rounded px-3 py-1.5;
}
button.secondary {
  @apply bg-slate-200 hover:bg-slate-300 text-slate-900;
}
button.link {
  @apply bg-transparent hover:bg-transparent text-blue-600 hover:underline px-0 py-0;
}
button.link.danger {
  @apply text-red-600;
}

input[type="text"],
input[type="search"],
input[type="email"],
input[type="tel"] {
  @apply border border-slate-300 rounded px-3 py-1.5;
}

table        { @apply w-full text-left; }
thead        { @apply bg-slate-100 text-sm text-slate-700; }
th, td       { @apply px-4 py-2; }
tbody tr     { @apply border-b border-slate-200 hover:bg-slate-50; }
tbody tr.editing { @apply bg-yellow-50; }

label { @apply block text-xs text-slate-600 mb-1; }
```

**Default button is primary.** A bare `<button>` is the filled-blue style. Variants are applied with the modifier classes `.secondary`, `.link`, `.danger`. `.link.danger` composes into a red link-style button.

**Edit-row tr** uses `class="editing"` instead of `class="bg-yellow-50"` so the styling belongs to the layer, not the markup.

## `@layer components` — small class set

```css
.card       { @apply bg-white border border-slate-200 rounded; }
.form-row   { @apply flex flex-wrap gap-2; }
.field      { @apply flex-1 min-w-[10rem]; }
.field input{ @apply w-full; }
.form-row > input { @apply flex-1 min-w-[10rem]; }
.actions    { @apply text-right; }
.muted      { @apply text-slate-600; }
.empty      { @apply px-4 py-6 text-center text-slate-500; }
```

Notes:
- `.form-row` does NOT include `items-*` — the consumer picks (`items-center` for inline inputs in the edit row, `items-end` for label-above-input in the add form).
- `.field` is for the label+input wrapper in the add form. Inputs inside a `.field` get `width: 100%` so the label and field share the same column.
- `.form-row > input` handles the inline-input case (edit row): unwrapped inputs flex to share the row.

## View-by-view changes

All view edits are class-only — no markup restructuring beyond adding the `.editing` and `.field` wrappers (the latter already exists, just being given a class name).

### `Shared/_ContactRow.cshtml`

| Element | Before (class=) | After (class=) |
|---|---|---|
| `<tr>` | `border-b border-slate-200 hover:bg-slate-50` | *(removed — element styling handles it)* |
| `<td>` Name | `px-4 py-2` | *(removed)* |
| `<td>` Email | `px-4 py-2 text-slate-600` | `muted` |
| `<td>` Phone | `px-4 py-2 text-slate-600` | `muted` |
| `<td>` actions | `px-4 py-2 text-right space-x-2` | `actions space-x-2` |
| Edit `<button>` | `text-blue-600 hover:underline` | `link` |
| Delete `<button>` | `text-red-600 hover:underline` | `link danger` |

### `Shared/_ContactEditRow.cshtml`

| Element | Before | After |
|---|---|---|
| `<tr>` | `border-b border-slate-200 bg-yellow-50` | `editing` |
| `<td colspan="4">` | `px-4 py-2` | *(removed)* |
| `<form>` | `flex flex-wrap gap-2 items-center` | `form-row items-center` |
| Inputs | `border border-slate-300 rounded px-2 py-1 flex-1 min-w-[10rem]` | *(removed — covered by element + `.form-row > input`)* |
| Save `<button>` | `bg-blue-600 hover:bg-blue-700 text-white rounded px-3 py-1` | *(removed — default primary)* |
| Cancel `<button>` | `bg-slate-200 hover:bg-slate-300 rounded px-3 py-1` | `secondary` |

### `Shared/_ContactList.cshtml`

| Element | Before | After |
|---|---|---|
| Empty row `<td>` | `px-4 py-6 text-center text-slate-500` | `empty` |

### `Contacts/Index.cshtml`

| Element | Before | After |
|---|---|---|
| Search `<input>` | `w-full border border-slate-300 rounded px-3 py-2` | `w-full` (input element styling covers the rest) |
| Add `<form>` | `flex flex-wrap gap-2 items-end bg-white p-4 border border-slate-200 rounded` | `card form-row items-end p-4` |
| Field wrappers `<div>` | `flex-1 min-w-[10rem]` | `field` |
| Field labels `<label>` | `block text-xs text-slate-600 mb-1` | *(removed — covered by `label` element style)* |
| Field inputs | `w-full border border-slate-300 rounded px-2 py-1` | *(removed — covered by `.field input` + element style)* |
| Add `<button>` | `bg-blue-600 hover:bg-blue-700 text-white rounded px-4 py-2` | *(removed — default primary)* |
| Table container `<div>` | `bg-white border border-slate-200 rounded overflow-hidden` | `card overflow-hidden` |
| `<table>` | `w-full text-left` | *(removed)* |
| `<thead>` | `bg-slate-100 text-sm text-slate-700` | *(removed)* |
| `<th>` Name/Email/Phone | `px-4 py-2` | *(removed)* |
| `<th>` Actions | `px-4 py-2 text-right` | `actions` |

### `Shared/_Layout.cshtml`

The `<header>`, `<h1>`, `<p>`, `<main>` styling stays as-is — they're not repeated patterns. Only the new `<style type="text/tailwindcss">` block is added.

The `<body>` keeps its `class="bg-slate-50 text-slate-900"` (also not a repeated pattern; lives on a single element).

## Intentional visual changes

These are not behavior changes but are listed so they're not surprises:

- **Buttons unify to one size** (`px-3 py-1.5`). The previous Add button was `px-4 py-2` and Save was `px-3 py-1`. The new Add is slightly smaller, the new Save slightly larger. Both look the same now.
- **Search input padding shifts from `px-3 py-2` to `px-3 py-1.5`** to match other inputs.
- **The `.editing` class makes the edit-row background coupling explicit** in the layer rather than inline. Same visual.

## Verification

The browser-MCP test sequence in `docs/superpowers/screenshots/` should still pass with the same observable behaviors:

1. Initial load: 5 seeded contacts.
2. Search "ada" → only Ada visible.
3. Add contact prepends new row.
4. Edit → form appears with the same yellow background.
5. Save → row returns with new values.
6. Cancel → row returns unchanged.
7. Delete → row removed.

The HTML smoke checks (rendered `hx-get`/`hx-post`/`hx-put`/`hx-delete` URLs) continue to pass because the tag helper output is unchanged.

A short manual sanity check at the end: take a screenshot before-and-after and eyeball the spacing — primary button size and search input padding are the two places small visual deltas could regress something.

## Failure mode

If the Tailwind Play CDN doesn't process the `<style type="text/tailwindcss">` block (e.g. the page renders with no styling on `<button>`/`<input>`), fall back to a plain `<style>` block with explicit CSS — no `@apply`, just the resolved utility values. The `@apply` is convenience, not necessity. This fallback is the contingency if the CDN behavior surprises us; the plan-phase will pick the explicit-CSS path immediately if the `@apply` path fails verification.
