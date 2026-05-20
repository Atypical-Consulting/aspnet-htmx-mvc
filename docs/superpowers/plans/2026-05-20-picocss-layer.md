# PicoCSS-flavored Class Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a small element + variant CSS layer to `_Layout.cshtml` and strip the resulting redundant Tailwind utility classes out of the four views, so each `.cshtml` reads like clean semantic HTML.

**Architecture:** One `<style type="text/tailwindcss">` block in `_Layout.cshtml` (processed at runtime by the Tailwind Play CDN; `@apply` and `@layer` work). Two layers: `@layer base` styles `button` / `input` / `table` / `th` / `td` / `tbody tr` / `label` element-globally, plus three button variant classes (`.secondary`, `.link`, `.danger`). `@layer components` adds `.card`, `.form-row`, `.field`, `.actions`, `.muted`, `.empty`. No new files, no new packages, no build step.

**Tech Stack:** Tailwind Play CDN, ASP.NET Core Razor views.

**Spec:** `docs/superpowers/specs/2026-05-20-picocss-layer-design.md`

**Testing approach:** The existing curl smoke test still passes (rendered HTML structure is preserved; only `class=` strings change). Tag-helper URL assertions from the previous task still pass. Visual verification via Chrome DevTools MCP at the end — load the page, screenshot, eyeball that buttons / inputs / table styling still look right.

**Fallback:** If the Tailwind Play CDN doesn't process `<style type="text/tailwindcss">` (rendered page shows unstyled buttons/inputs), switch the same selectors to plain CSS rules in a regular `<style>` block. The spec calls this out; the recovery step is in Task 1.

---

## File Structure

```
src/HtmxMvc.Web/Views/
  Shared/
    _Layout.cshtml             [Task 1, MODIFY — add <style type="text/tailwindcss"> block]
    _ContactRow.cshtml         [Task 2, MODIFY]
    _ContactEditRow.cshtml     [Task 2, MODIFY]
    _ContactList.cshtml        [Task 2, MODIFY]
  Contacts/
    Index.cshtml               [Task 2, MODIFY]
docs/superpowers/screenshots/
  07-picocss-layer.png         [Task 3, NEW]
```

No new C# files, no new view files, no new packages, no DI changes.

---

## Task 1: Add the CSS layer to `_Layout.cshtml`

Adds the style block. Views still have all their existing inline classes so visual behavior is unchanged at this point.

**Files:**
- Modify: `src/HtmxMvc.Web/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Insert the `<style type="text/tailwindcss">` block**

In `src/HtmxMvc.Web/Views/Shared/_Layout.cshtml`, find:
```cshtml
    <script src="https://cdn.tailwindcss.com"></script>
    <script src="https://unpkg.com/htmx.org@@2.0.10/dist/htmx.js"></script>
```

Insert a new `<style>` block between those two lines:
```cshtml
    <script src="https://cdn.tailwindcss.com"></script>
    <style type="text/tailwindcss">
        @@layer base {
            button {
                @@apply bg-blue-600 hover:bg-blue-700 text-white rounded px-3 py-1.5;
            }
            button.secondary {
                @@apply bg-slate-200 hover:bg-slate-300 text-slate-900;
            }
            button.link {
                @@apply bg-transparent hover:bg-transparent text-blue-600 hover:underline px-0 py-0;
            }
            button.link.danger {
                @@apply text-red-600;
            }

            input[type="text"],
            input[type="search"],
            input[type="email"],
            input[type="tel"] {
                @@apply border border-slate-300 rounded px-3 py-1.5;
            }

            table        { @@apply w-full text-left; }
            thead        { @@apply bg-slate-100 text-sm text-slate-700; }
            th, td       { @@apply px-4 py-2; }
            tbody tr     { @@apply border-b border-slate-200 hover:bg-slate-50; }
            tbody tr.editing { @@apply bg-yellow-50; }

            label { @@apply block text-xs text-slate-600 mb-1; }
        }

        @@layer components {
            .card       { @@apply bg-white border border-slate-200 rounded; }
            .form-row   { @@apply flex flex-wrap gap-2; }
            .field      { @@apply flex-1 min-w-[10rem]; }
            .field input{ @@apply w-full; }
            .form-row > input { @@apply flex-1 min-w-[10rem]; }
            .actions    { @@apply text-right; }
            .muted      { @@apply text-slate-600; }
            .empty      { @@apply px-4 py-6 text-center text-slate-500; }
        }
    </style>
    <script src="https://unpkg.com/htmx.org@@2.0.10/dist/htmx.js"></script>
```

Razor note: every `@` literal in the CSS must be written as `@@` to escape it for the Razor parser. That covers `@layer`, `@apply`, and the `@2.0.10` in the existing script tag. The two existing `@@2.0.10` references are already escaped — leave them as-is.

- [ ] **Step 2: Build to verify Razor still compiles**

Run from repo root:
```powershell
dotnet build src/HtmxMvc.Web
```
Expected: `Build succeeded` with 0 errors. If you see a Razor error about `@layer` or `@apply`, you missed an `@@` escape — fix and rebuild.

- [ ] **Step 3: Smoke check the page still renders**

Start the app in the background:
```powershell
dotnet run --project src/HtmxMvc.Web --urls http://localhost:5099
```

Wait for `Now listening on: http://localhost:5099`, then:
```bash
curl -sf http://localhost:5099/ -o /tmp/idx.html && echo "ok: page renders"
grep -qF '@apply bg-blue-600' /tmp/idx.html && echo "ok: style block was sent to browser" || echo "FAIL: style block missing"
```

Expected: both lines `ok`. The block is sent to the browser as a `<style type="text/tailwindcss">`; the Play CDN's runtime processes it after page load. We can't fully verify the layer is *applied* until Task 3 (which inspects computed styles in a real browser), because the views still have their inline Tailwind classes and would render correctly even if the layer was inert.

Stop the app.

- [ ] **Step 4: Commit**

```powershell
git add src/HtmxMvc.Web/Views/Shared/_Layout.cshtml
git commit -m "$(@'
feat: add element-styling + variant-class layer in _Layout

Bare <button> becomes filled-blue primary; .secondary / .link / .danger
variants compose. Inputs, tables, ths, tds, tbody rows get sensible
defaults. Component classes (.card, .form-row, .field, .actions,
.muted, .empty) cover the structural pieces element styling cannot.

Views are not yet refactored, so the page still looks identical to
before. The next commit strips the redundant inline classes.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@)"
```

---

## Task 2: Refactor the four views

All edits are class-only — no markup restructuring. The element + component layer from Task 1 supplies the previously-inline styling.

**Files:**
- Modify: `src/HtmxMvc.Web/Views/Shared/_ContactRow.cshtml`
- Modify: `src/HtmxMvc.Web/Views/Shared/_ContactEditRow.cshtml`
- Modify: `src/HtmxMvc.Web/Views/Shared/_ContactList.cshtml`
- Modify: `src/HtmxMvc.Web/Views/Contacts/Index.cshtml`

- [ ] **Step 1: Rewrite `_ContactRow.cshtml`**

Replace the entire contents of `src/HtmxMvc.Web/Views/Shared/_ContactRow.cshtml` with:
```cshtml
@model HtmxMvc.Domain.Contact
<tr id="contact-@Model.Id">
    <td>@Model.Name</td>
    <td class="muted">@Model.Email</td>
    <td class="muted">@Model.Phone</td>
    <td class="actions space-x-2">
        <button class="link"
                hx-action="Edit"
                hx-route-id="@Model.Id"
                hx-target="#contact-@Model.Id"
                hx-swap="outerHTML">Edit</button>
        <button class="link danger"
                hx-action="Delete"
                hx-route-id="@Model.Id"
                hx-target="#contact-@Model.Id"
                hx-swap="outerHTML"
                hx-confirm="Delete @Model.Name?">Delete</button>
    </td>
</tr>
```

- [ ] **Step 2: Rewrite `_ContactEditRow.cshtml`**

Replace the entire contents of `src/HtmxMvc.Web/Views/Shared/_ContactEditRow.cshtml` with:
```cshtml
@model HtmxMvc.Domain.Contact
<tr id="contact-@Model.Id" class="editing">
    <td colspan="4">
        <form hx-action="Update"
              hx-route-id="@Model.Id"
              hx-target="#contact-@Model.Id"
              hx-swap="outerHTML"
              class="form-row items-center">
            <input name="Name" value="@Model.Name" required placeholder="Name" />
            <input name="Email" value="@Model.Email" placeholder="Email" />
            <input name="Phone" value="@Model.Phone" placeholder="Phone" />
            <button type="submit">Save</button>
            <button type="button"
                    class="secondary"
                    hx-action="Row"
                    hx-route-id="@Model.Id"
                    hx-target="#contact-@Model.Id"
                    hx-swap="outerHTML">Cancel</button>
        </form>
    </td>
</tr>
```

Note: the `<input>` elements default to `type="text"`, which is in our element selector list — they get the bordered/rounded/padding styling automatically. Same for `placeholder=` (purely cosmetic carryover).

- [ ] **Step 3: Rewrite `_ContactList.cshtml`**

Replace the entire contents of `src/HtmxMvc.Web/Views/Shared/_ContactList.cshtml` with:
```cshtml
@model IReadOnlyList<HtmxMvc.Domain.Contact>
@if (Model.Count == 0)
{
    <tr><td colspan="4" class="empty">No contacts found.</td></tr>
}
else
{
    foreach (var contact in Model)
    {
        <partial name="_ContactRow" model="contact" />
    }
}
```

- [ ] **Step 4: Rewrite `Contacts/Index.cshtml`**

Replace the entire contents of `src/HtmxMvc.Web/Views/Contacts/Index.cshtml` with:
```cshtml
@model IReadOnlyList<HtmxMvc.Domain.Contact>
@{
    ViewData["Title"] = "Contacts";
}

<section class="space-y-6">
    <div>
        <label for="q">Search</label>
        <input id="q" type="search" name="q"
               placeholder="Type to filter..."
               class="w-full"
               hx-action="List"
               hx-trigger="keyup changed delay:300ms, search"
               hx-target="#contact-rows"
               hx-swap="innerHTML" />
    </div>

    <form hx-action="Create"
          hx-target="#contact-rows"
          hx-swap="afterbegin"
          hx-on::after-request="if(event.detail.successful) this.reset()"
          class="card form-row items-end p-4">
        <div class="field">
            <label>Name</label>
            <input name="Name" required />
        </div>
        <div class="field">
            <label>Email</label>
            <input name="Email" />
        </div>
        <div class="field">
            <label>Phone</label>
            <input name="Phone" />
        </div>
        <button type="submit">Add</button>
    </form>

    <div class="card overflow-hidden">
        <table>
            <thead>
                <tr>
                    <th>Name</th>
                    <th>Email</th>
                    <th>Phone</th>
                    <th class="actions">Actions</th>
                </tr>
            </thead>
            <tbody id="contact-rows">
                <partial name="_ContactList" model="Model" />
            </tbody>
        </table>
    </div>
</section>
```

- [ ] **Step 5: Build to verify Razor still compiles**

Run: `dotnet build`
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 6: Curl smoke test — rendered URLs and structure still match**

Start the app in the background:
```powershell
dotnet run --project src/HtmxMvc.Web --urls http://localhost:5099
```

Wait for `Now listening on: http://localhost:5099`, then verify the rendered HTML still has the right URLs and structure:
```bash
curl -s http://localhost:5099/ -o /tmp/idx.html

echo "--- seeded contacts still present ---"
for name in "Ada Lovelace" "Alan Turing" "Grace Hopper" "Edsger Dijkstra" "Margaret Hamilton"; do
  if grep -qF "$name" /tmp/idx.html; then echo "ok: $name"; else echo "FAIL: $name"; fi
done

echo "--- tag-helper URLs unchanged ---"
grep -qF 'hx-get="/contacts/list"' /tmp/idx.html && echo "ok: search" || echo "FAIL"
grep -qF 'hx-post="/contacts"' /tmp/idx.html && echo "ok: add" || echo "FAIL"
grep -qF 'hx-get="/contacts/1/edit"' /tmp/idx.html && echo "ok: edit-1" || echo "FAIL"
grep -qF 'hx-delete="/contacts/1"' /tmp/idx.html && echo "ok: delete-1" || echo "FAIL"

echo "--- new class names present ---"
grep -qF 'class="link"' /tmp/idx.html && echo "ok: link class" || echo "FAIL"
grep -qF 'class="link danger"' /tmp/idx.html && echo "ok: link danger" || echo "FAIL"
grep -qF 'class="muted"' /tmp/idx.html && echo "ok: muted" || echo "FAIL"
grep -qF 'class="actions"' /tmp/idx.html && echo "ok: actions th" || echo "FAIL"
grep -qF 'class="card form-row items-end p-4"' /tmp/idx.html && echo "ok: add-form classes" || echo "FAIL"
grep -qF 'class="field"' /tmp/idx.html && echo "ok: field" || echo "FAIL"
grep -qF 'class="card overflow-hidden"' /tmp/idx.html && echo "ok: table card" || echo "FAIL"

echo "--- old Tailwind strings are gone ---"
grep -qF 'text-blue-600 hover:underline' /tmp/idx.html && echo "FAIL: link tailwind leaked" || echo "ok"
grep -qF 'bg-blue-600 hover:bg-blue-700' /tmp/idx.html && echo "FAIL: primary tailwind leaked" || echo "ok"
grep -qF 'bg-slate-200 hover:bg-slate-300' /tmp/idx.html && echo "FAIL: secondary tailwind leaked" || echo "ok"
grep -qF 'border-slate-300' /tmp/idx.html && echo "FAIL: input tailwind leaked" || echo "ok"
```

Expected: every line ends in `ok`. No `FAIL`.

- [ ] **Step 7: Full CRUD smoke test still works**

```bash
JAR=/tmp/picocss-cookies.txt
rm -f $JAR
curl -sc $JAR http://localhost:5099/ -o /tmp/idx2.html
TOKEN=$(grep -oE 'RequestVerificationToken&quot;: &quot;[A-Za-z0-9_\-]+' /tmp/idx2.html | head -1 | sed 's/.*&quot;//')

echo "POST:"
curl -sb $JAR -X POST "http://localhost:5099/contacts" -H "RequestVerificationToken: $TOKEN" \
  --data-urlencode "Name=Style Layer Test" --data-urlencode "Email=s@l.test" --data-urlencode "Phone=000" \
  -o /tmp/new.html -w "status=%{http_code}\n"
NEWID=$(grep -oE 'id="contact-[0-9]+"' /tmp/new.html | head -1 | grep -oE '[0-9]+')
echo "new id: $NEWID"

echo "DELETE:"
curl -sb $JAR -X DELETE "http://localhost:5099/contacts/$NEWID" -H "RequestVerificationToken: $TOKEN" \
  -w "status=%{http_code}\n" -o /dev/null
```

Expected: POST 200 with new id, DELETE 200.

Stop the app.

- [ ] **Step 8: Commit**

```powershell
git add src/HtmxMvc.Web/Views
git commit -m "$(@'
refactor: strip repeated Tailwind utility classes from views

Buttons, inputs, table cells, thead/tbody rows now inherit styling
from the element selectors added in the previous commit. Class
attributes shrink to variant names: .link / .link.danger / .secondary
for buttons, .muted / .actions / .empty for cells, .card / .form-row /
.field for structural containers. Edit-row uses .editing instead of
inline bg-yellow-50.

Markup is significantly leaner and reads as semantic HTML.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@)"
```

---

## Task 3: Visual verification via Chrome DevTools MCP

This task confirms the page actually renders correctly — the curl checks only verify HTML structure, not pixels.

**Files:**
- Create: `docs/superpowers/screenshots/07-picocss-layer.png`

- [ ] **Step 1: Start the app**

```powershell
dotnet run --project src/HtmxMvc.Web --urls http://localhost:5099
```

Wait for `Now listening on: http://localhost:5099`.

- [ ] **Step 2: Open the page in Chrome DevTools MCP**

Using the Chrome DevTools MCP tools (loaded in the previous browser session):
- `new_page url=http://localhost:5099/`
- `take_snapshot` — verify the snapshot shows all five seeded contacts, an Edit and Delete button on each row, a Search input, an Add form with three labeled fields and an Add button.

- [ ] **Step 3: Verify primary button styling is filled blue**

`evaluate_script` with the function:
```javascript
() => {
  const addButton = [...document.querySelectorAll('button')].find(b => b.textContent.trim() === 'Add');
  if (!addButton) return 'FAIL: Add button not found';
  const cs = getComputedStyle(addButton);
  return { bg: cs.backgroundColor, color: cs.color, padding: cs.padding };
}
```

Expected: `bg` is `rgb(37, 99, 235)` (blue-600), `color` is `rgb(255, 255, 255)` (white). If `bg` is `rgba(0, 0, 0, 0)` or default, the layer isn't applying — investigate.

- [ ] **Step 4: Verify link-style buttons are blue text, no background**

`evaluate_script`:
```javascript
() => {
  const editButton = [...document.querySelectorAll('button')].find(b => b.textContent.trim() === 'Edit');
  if (!editButton) return 'FAIL: Edit button not found';
  const cs = getComputedStyle(editButton);
  return { bg: cs.backgroundColor, color: cs.color };
}
```

Expected: `bg` is `rgba(0, 0, 0, 0)` (transparent), `color` is `rgb(37, 99, 235)` (blue-600).

- [ ] **Step 5: Verify Delete button is red link-style**

`evaluate_script`:
```javascript
() => {
  const deleteButton = [...document.querySelectorAll('button')].find(b => b.textContent.trim() === 'Delete');
  if (!deleteButton) return 'FAIL: Delete button not found';
  const cs = getComputedStyle(deleteButton);
  return { color: cs.color };
}
```

Expected: `color` is `rgb(220, 38, 38)` (red-600).

- [ ] **Step 6: Take a full-page screenshot**

`take_screenshot filePath="C:/repo/poc/HtmxMvc/docs/superpowers/screenshots/07-picocss-layer.png" fullPage=true`

- [ ] **Step 7: Smoke-test interactions still work in-browser**

Run a quick interaction sequence using Chrome DevTools MCP:
1. `take_snapshot` to get fresh uids
2. Click an Edit button (e.g. on Ada's row) — verify a yellow `editing` row appears with three inputs + Save + Cancel
3. Click Cancel — verify the original row returns
4. Click Delete on a different row, accept the confirm dialog — verify the row disappears

If any of these don't visually behave as expected, the new CSS broke something. Investigate before continuing.

- [ ] **Step 8: Stop the app and commit the screenshot**

Stop the dotnet run background task, then:
```powershell
git add docs/superpowers/screenshots/07-picocss-layer.png docs/superpowers/specs/2026-05-20-picocss-layer-design.md docs/superpowers/plans/2026-05-20-picocss-layer.md
git commit -m "$(@'
docs: add picocss-layer screenshot and design + plan docs

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@)"
```

---

## Done

After Task 3, the views are significantly leaner and the page renders identically (with the small intentional deltas around button sizing called out in the spec). The diff for Task 2 should be mostly red — class attribute strings shrinking from 40+ chars to 4-15 chars on average.

```powershell
dotnet test
dotnet run --project src/HtmxMvc.Web
```
