# HTMX Contacts Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a single-page ASP.NET Core MVC (.NET 10) contacts app demonstrating six HTMX 2 patterns (active search, append-on-create, inline edit, swap-back-on-cancel, in-place update, delete-row) with an in-memory store and Tailwind via CDN.

**Architecture:** Standard MVC project. One controller (`ContactsController`) serves one full page (`Index`) plus six partial-returning actions. A thread-safe singleton `ContactService` holds an in-memory list seeded at startup. HTMX attributes on the views drive all server interactions; zero custom client-side JS.

**Tech Stack:** ASP.NET Core MVC on `net10.0`, Razor views, HTMX 2.0.4 (CDN), Tailwind Play CDN.

**Spec:** `docs/superpowers/specs/2026-05-19-htmx-contacts-demo-design.md`

**Testing approach:** Per spec, no unit/integration test project. Verification is `dotnet build` + a curl-based smoke test of the running app at the end. Each task commits independently.

---

## File Structure

```
HtmxMvc.sln                                 [Task 1]
src/HtmxMvc/
  HtmxMvc.csproj                            [Task 1]
  Program.cs                                [Task 1, 3, 6]
  appsettings.json                          [Task 1, from template]
  appsettings.Development.json              [Task 1, from template]
  Properties/launchSettings.json            [Task 1, from template]
  Models/
    Contact.cs                              [Task 2]
  Services/
    ContactService.cs                       [Task 3]
  Controllers/
    ContactsController.cs                   [Task 6]
  Views/
    _ViewImports.cshtml                     [Task 4]
    _ViewStart.cshtml                       [Task 4]
    Shared/
      _Layout.cshtml                        [Task 4]
      _ContactRow.cshtml                    [Task 5]
      _ContactEditRow.cshtml                [Task 5]
      _ContactList.cshtml                   [Task 5]
    Contacts/
      Index.cshtml                          [Task 6]
  wwwroot/                                  [empty, default static-files dir]
```

---

## Task 1: Scaffold solution and project

**Files:**
- Create: `HtmxMvc.sln`
- Create: `src/HtmxMvc/HtmxMvc.csproj`
- Create (from template): `src/HtmxMvc/Program.cs`, `appsettings*.json`, `Properties/launchSettings.json`, `Views/Home/*`, `Views/Shared/*`, etc. (template content; we will overwrite/delete files in later tasks)

- [ ] **Step 1: Create solution and MVC project**

Run from `C:\repo\poc\HtmxMvc`:
```powershell
dotnet new sln -n HtmxMvc
dotnet new mvc -n HtmxMvc -o src/HtmxMvc --framework net10.0
dotnet sln add src/HtmxMvc/HtmxMvc.csproj
```

Expected: solution file + `src/HtmxMvc/` with template MVC project. The csproj should contain `<TargetFramework>net10.0</TargetFramework>`.

- [ ] **Step 2: Remove default Home controller and views (will be replaced by Contacts)**

Delete:
- `src/HtmxMvc/Controllers/HomeController.cs`
- `src/HtmxMvc/Views/Home/` (entire folder)
- `src/HtmxMvc/Models/ErrorViewModel.cs`
- `src/HtmxMvc/Views/Shared/Error.cshtml`

```powershell
Remove-Item src/HtmxMvc/Controllers/HomeController.cs
Remove-Item src/HtmxMvc/Views/Home -Recurse
Remove-Item src/HtmxMvc/Models/ErrorViewModel.cs
Remove-Item src/HtmxMvc/Views/Shared/Error.cshtml
```

- [ ] **Step 3: Verify build still works**

Run: `dotnet build`
Expected: `Build succeeded`. (Program.cs and remaining template files still compile — we haven't touched routing yet.)

- [ ] **Step 4: Commit**

```powershell
git init
git add .gitignore HtmxMvc.sln src/HtmxMvc
git commit -m "chore: scaffold ASP.NET Core MVC project on net10.0"
```

Note: if `.gitignore` doesn't exist, run `dotnet new gitignore` first.

---

## Task 2: Add `Contact` model

**Files:**
- Create: `src/HtmxMvc/Models/Contact.cs`

- [ ] **Step 1: Write the model**

`src/HtmxMvc/Models/Contact.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace HtmxMvc.Models;

public sealed class Contact
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(200)]
    public string Email { get; set; } = "";

    [StringLength(50)]
    public string Phone { get; set; } = "";
}
```

Note: Uses `class` (not `record`) so MVC model binding works cleanly with the parameterless constructor and mutable properties. `Id` is mutable so the service can assign it on `Add`.

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```powershell
git add src/HtmxMvc/Models/Contact.cs
git commit -m "feat: add Contact model with validation attributes"
```

---

## Task 3: Add `ContactService` and register as singleton

**Files:**
- Create: `src/HtmxMvc/Services/ContactService.cs`
- Modify: `src/HtmxMvc/Program.cs`

- [ ] **Step 1: Write the service**

`src/HtmxMvc/Services/ContactService.cs`:
```csharp
using HtmxMvc.Models;

namespace HtmxMvc.Services;

public sealed class ContactService
{
    private readonly object _gate = new();
    private readonly List<Contact> _contacts = new();
    private int _nextId = 1;

    public ContactService()
    {
        Add(new Contact { Name = "Ada Lovelace",     Email = "ada@analyticalengine.org",  Phone = "555-0101" });
        Add(new Contact { Name = "Alan Turing",      Email = "alan@bletchley.uk",         Phone = "555-0102" });
        Add(new Contact { Name = "Grace Hopper",     Email = "grace@navy.mil",            Phone = "555-0103" });
        Add(new Contact { Name = "Edsger Dijkstra",  Email = "edsger@eindhoven.nl",       Phone = "555-0104" });
        Add(new Contact { Name = "Margaret Hamilton", Email = "margaret@apollo.nasa.gov", Phone = "555-0105" });
    }

    public IReadOnlyList<Contact> GetAll()
    {
        lock (_gate)
        {
            return _contacts.OrderBy(c => c.Id).ToList();
        }
    }

    public IReadOnlyList<Contact> Search(string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return GetAll();
        lock (_gate)
        {
            return _contacts
                .Where(c =>
                    c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    c.Phone.Contains(q, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Id)
                .ToList();
        }
    }

    public Contact? Get(int id)
    {
        lock (_gate)
        {
            return _contacts.FirstOrDefault(c => c.Id == id);
        }
    }

    public Contact Add(Contact c)
    {
        lock (_gate)
        {
            c.Id = _nextId++;
            _contacts.Add(c);
            return c;
        }
    }

    public Contact? Update(int id, Contact c)
    {
        lock (_gate)
        {
            var existing = _contacts.FirstOrDefault(x => x.Id == id);
            if (existing is null) return null;
            existing.Name = c.Name;
            existing.Email = c.Email;
            existing.Phone = c.Phone;
            return existing;
        }
    }

    public bool Delete(int id)
    {
        lock (_gate)
        {
            var existing = _contacts.FirstOrDefault(x => x.Id == id);
            if (existing is null) return false;
            _contacts.Remove(existing);
            return true;
        }
    }
}
```

- [ ] **Step 2: Register the service in `Program.cs`**

Open `src/HtmxMvc/Program.cs`. Find the line:
```csharp
builder.Services.AddControllersWithViews();
```

Add **immediately after** it:
```csharp
builder.Services.AddSingleton<HtmxMvc.Services.ContactService>();
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```powershell
git add src/HtmxMvc/Services/ContactService.cs src/HtmxMvc/Program.cs
git commit -m "feat: add in-memory ContactService with seed data"
```

---

## Task 4: Replace `_Layout.cshtml` and view imports for HTMX + Tailwind

**Files:**
- Modify: `src/HtmxMvc/Views/Shared/_Layout.cshtml`
- Modify: `src/HtmxMvc/Views/_ViewImports.cshtml`
- Verify: `src/HtmxMvc/Views/_ViewStart.cshtml` (no change expected)

- [ ] **Step 1: Update `_ViewImports.cshtml`**

Replace the contents of `src/HtmxMvc/Views/_ViewImports.cshtml` with:
```cshtml
@using HtmxMvc
@using HtmxMvc.Models
@using HtmxMvc.Services
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

- [ ] **Step 2: Replace `_Layout.cshtml`**

Replace the contents of `src/HtmxMvc/Views/Shared/_Layout.cshtml` with:
```cshtml
@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery
@{
    var tokens = Antiforgery.GetAndStoreTokens(Context);
    var antiforgeryToken = tokens.RequestToken;
    var antiforgeryHeader = tokens.HeaderName;
}
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - HtmxMvc</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <script src="https://unpkg.com/htmx.org@2.0.4"></script>
    <!-- SRI omitted intentionally: pin to a vetted hash before any non-demo use. -->

</head>
<body class="bg-slate-50 text-slate-900"
      hx-headers='@($"{{\"{antiforgeryHeader}\": \"{antiforgeryToken}\"}}")'>
    <header class="bg-white border-b border-slate-200">
        <div class="max-w-4xl mx-auto px-6 py-4">
            <h1 class="text-2xl font-semibold">HTMX Contacts Demo</h1>
            <p class="text-sm text-slate-500">ASP.NET Core MVC on .NET 10 + HTMX 2</p>
        </div>
    </header>
    <main class="max-w-4xl mx-auto px-6 py-8">
        @RenderBody()
    </main>
</body>
</html>
```

Notes:
- The `integrity` value above is a placeholder — if browser blocks the script, remove the `integrity` and `crossorigin` attributes. (Use Subresource Integrity only if you've verified the hash.)
- The antiforgery token is injected into a request header on every HTMX request via `hx-headers` on `<body>` (children inherit).

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```powershell
git add src/HtmxMvc/Views/_ViewImports.cshtml src/HtmxMvc/Views/Shared/_Layout.cshtml
git commit -m "feat: wire HTMX 2 + Tailwind CDN into _Layout with antiforgery header"
```

---

## Task 5: Add row partials

**Files:**
- Create: `src/HtmxMvc/Views/Shared/_ContactRow.cshtml`
- Create: `src/HtmxMvc/Views/Shared/_ContactEditRow.cshtml`
- Create: `src/HtmxMvc/Views/Shared/_ContactList.cshtml`

- [ ] **Step 1: Create `_ContactRow.cshtml`**

`src/HtmxMvc/Views/Shared/_ContactRow.cshtml`:
```cshtml
@model HtmxMvc.Models.Contact
<tr id="contact-@Model.Id" class="border-b border-slate-200 hover:bg-slate-50">
    <td class="px-4 py-2">@Model.Name</td>
    <td class="px-4 py-2 text-slate-600">@Model.Email</td>
    <td class="px-4 py-2 text-slate-600">@Model.Phone</td>
    <td class="px-4 py-2 text-right space-x-2">
        <button class="text-blue-600 hover:underline"
                hx-get="/contacts/@Model.Id/edit"
                hx-target="#contact-@Model.Id"
                hx-swap="outerHTML">Edit</button>
        <button class="text-red-600 hover:underline"
                hx-delete="/contacts/@Model.Id"
                hx-target="#contact-@Model.Id"
                hx-swap="outerHTML"
                hx-confirm="Delete @Model.Name?">Delete</button>
    </td>
</tr>
```

- [ ] **Step 2: Create `_ContactEditRow.cshtml`**

`src/HtmxMvc/Views/Shared/_ContactEditRow.cshtml`:
```cshtml
@model HtmxMvc.Models.Contact
<tr id="contact-@Model.Id" class="border-b border-slate-200 bg-yellow-50">
    <td colspan="4" class="px-4 py-2">
        <form hx-put="/contacts/@Model.Id"
              hx-target="#contact-@Model.Id"
              hx-swap="outerHTML"
              class="flex flex-wrap gap-2 items-center">
            <input name="Name" value="@Model.Name" required
                   class="border border-slate-300 rounded px-2 py-1 flex-1 min-w-[10rem]"
                   placeholder="Name" />
            <input name="Email" value="@Model.Email"
                   class="border border-slate-300 rounded px-2 py-1 flex-1 min-w-[10rem]"
                   placeholder="Email" />
            <input name="Phone" value="@Model.Phone"
                   class="border border-slate-300 rounded px-2 py-1 flex-1 min-w-[8rem]"
                   placeholder="Phone" />
            <button type="submit"
                    class="bg-blue-600 hover:bg-blue-700 text-white rounded px-3 py-1">Save</button>
            <button type="button"
                    hx-get="/contacts/@Model.Id"
                    hx-target="#contact-@Model.Id"
                    hx-swap="outerHTML"
                    class="bg-slate-200 hover:bg-slate-300 rounded px-3 py-1">Cancel</button>
        </form>
    </td>
</tr>
```

- [ ] **Step 3: Create `_ContactList.cshtml`**

`src/HtmxMvc/Views/Shared/_ContactList.cshtml`:
```cshtml
@model IReadOnlyList<HtmxMvc.Models.Contact>
@if (Model.Count == 0)
{
    <tr><td colspan="4" class="px-4 py-6 text-center text-slate-500">No contacts found.</td></tr>
}
else
{
    foreach (var contact in Model)
    {
        <partial name="_ContactRow" model="contact" />
    }
}
```

- [ ] **Step 4: Verify build**

Run: `dotnet build`
Expected: `Build succeeded`. (Razor compiles the partials.)

- [ ] **Step 5: Commit**

```powershell
git add src/HtmxMvc/Views/Shared/_ContactRow.cshtml src/HtmxMvc/Views/Shared/_ContactEditRow.cshtml src/HtmxMvc/Views/Shared/_ContactList.cshtml
git commit -m "feat: add contact row/edit/list partials with HTMX wiring"
```

---

## Task 6: Add `ContactsController`, `Index` view, and routing

**Files:**
- Create: `src/HtmxMvc/Controllers/ContactsController.cs`
- Create: `src/HtmxMvc/Views/Contacts/Index.cshtml`
- Modify: `src/HtmxMvc/Program.cs` (default route → Contacts)

- [ ] **Step 1: Create the controller**

`src/HtmxMvc/Controllers/ContactsController.cs`:
```csharp
using HtmxMvc.Models;
using HtmxMvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace HtmxMvc.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class ContactsController : Controller
{
    private readonly ContactService _service;

    public ContactsController(ContactService service) => _service = service;

    // GET /
    [HttpGet("/")]
    public IActionResult Index()
    {
        return View(_service.GetAll());
    }

    // GET /contacts/list?q=...
    [HttpGet("/contacts/list")]
    public IActionResult List(string? q)
    {
        return PartialView("_ContactList", _service.Search(q));
    }

    // POST /contacts
    [HttpPost("/contacts")]
    public IActionResult Create(Contact contact)
    {
        if (!ModelState.IsValid)
        {
            // Out of scope for this demo: surface ModelState in the form.
            // For now, return 400 — the form stays as-is on the client.
            return BadRequest();
        }
        var created = _service.Add(new Contact
        {
            Name = contact.Name,
            Email = contact.Email,
            Phone = contact.Phone
        });
        return PartialView("_ContactRow", created);
    }

    // GET /contacts/{id}/edit
    [HttpGet("/contacts/{id:int}/edit")]
    public IActionResult Edit(int id)
    {
        var c = _service.Get(id);
        return c is null ? NotFound() : PartialView("_ContactEditRow", c);
    }

    // PUT /contacts/{id}
    [HttpPut("/contacts/{id:int}")]
    public IActionResult Update(int id, Contact contact)
    {
        if (!ModelState.IsValid) return BadRequest();
        var updated = _service.Update(id, contact);
        return updated is null ? NotFound() : PartialView("_ContactRow", updated);
    }

    // GET /contacts/{id}
    [HttpGet("/contacts/{id:int}")]
    public IActionResult Row(int id)
    {
        var c = _service.Get(id);
        return c is null ? NotFound() : PartialView("_ContactRow", c);
    }

    // DELETE /contacts/{id}
    [HttpDelete("/contacts/{id:int}")]
    public IActionResult Delete(int id)
    {
        return _service.Delete(id) ? Ok() : NotFound();
    }
}
```

Notes:
- `[AutoValidateAntiforgeryToken]` enforces antiforgery on POST/PUT/DELETE; the `hx-headers` on `<body>` in `_Layout.cshtml` supplies the token.
- Attribute routing on every action — the default conventional route is removed in Step 3.

- [ ] **Step 2: Create the `Index` view**

`src/HtmxMvc/Views/Contacts/Index.cshtml`:
```cshtml
@model IReadOnlyList<HtmxMvc.Models.Contact>
@{
    ViewData["Title"] = "Contacts";
}

<section class="space-y-6">
    <div>
        <label for="q" class="block text-sm font-medium text-slate-700 mb-1">Search</label>
        <input id="q" type="search" name="q"
               placeholder="Type to filter..."
               class="w-full border border-slate-300 rounded px-3 py-2"
               hx-get="/contacts/list"
               hx-trigger="keyup changed delay:300ms, search"
               hx-target="#contact-rows"
               hx-swap="innerHTML" />
    </div>

    <form hx-post="/contacts"
          hx-target="#contact-rows"
          hx-swap="afterbegin"
          hx-on::after-request="if(event.detail.successful) this.reset()"
          class="flex flex-wrap gap-2 items-end bg-white p-4 border border-slate-200 rounded">
        <div class="flex-1 min-w-[10rem]">
            <label class="block text-xs text-slate-600 mb-1">Name</label>
            <input name="Name" required
                   class="w-full border border-slate-300 rounded px-2 py-1" />
        </div>
        <div class="flex-1 min-w-[10rem]">
            <label class="block text-xs text-slate-600 mb-1">Email</label>
            <input name="Email"
                   class="w-full border border-slate-300 rounded px-2 py-1" />
        </div>
        <div class="flex-1 min-w-[8rem]">
            <label class="block text-xs text-slate-600 mb-1">Phone</label>
            <input name="Phone"
                   class="w-full border border-slate-300 rounded px-2 py-1" />
        </div>
        <button type="submit"
                class="bg-blue-600 hover:bg-blue-700 text-white rounded px-4 py-2">Add</button>
    </form>

    <div class="bg-white border border-slate-200 rounded overflow-hidden">
        <table class="w-full text-left">
            <thead class="bg-slate-100 text-sm text-slate-700">
                <tr>
                    <th class="px-4 py-2">Name</th>
                    <th class="px-4 py-2">Email</th>
                    <th class="px-4 py-2">Phone</th>
                    <th class="px-4 py-2 text-right">Actions</th>
                </tr>
            </thead>
            <tbody id="contact-rows">
                <partial name="_ContactList" model="Model" />
            </tbody>
        </table>
    </div>
</section>
```

- [ ] **Step 3: Update `Program.cs` routing**

Open `src/HtmxMvc/Program.cs`. Find the line:
```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

Replace with:
```csharp
app.MapControllers();
```

This switches to attribute-routing only, which matches the `[HttpGet("/")]` etc. on `ContactsController`.

- [ ] **Step 4: Verify build**

Run: `dotnet build`
Expected: `Build succeeded`.

- [ ] **Step 5: Commit**

```powershell
git add src/HtmxMvc/Controllers/ContactsController.cs src/HtmxMvc/Views/Contacts/Index.cshtml src/HtmxMvc/Program.cs
git commit -m "feat: add ContactsController with HTMX partial actions and Index view"
```

---

## Task 7: Smoke-test the running app

**Files:** none

- [ ] **Step 1: Run the app in the background**

```powershell
dotnet run --project src/HtmxMvc --urls http://localhost:5099
```

Run this in a background shell. Wait until you see `Now listening on: http://localhost:5099`.

- [ ] **Step 2: Verify the index page renders with seeded contacts**

Run:
```powershell
curl -s http://localhost:5099/
```

Expected: HTTP 200; response body contains all five seeded names: `Ada Lovelace`, `Alan Turing`, `Grace Hopper`, `Edsger Dijkstra`, `Margaret Hamilton`. The body should also reference `htmx.org@2.0.4` and `cdn.tailwindcss.com`.

- [ ] **Step 3: Verify the search partial works**

Run:
```powershell
curl -s "http://localhost:5099/contacts/list?q=ada"
```

Expected: HTTP 200; response is a `<tr>` fragment (not a full HTML page) containing `Ada Lovelace` and NOT containing `Alan Turing`.

- [ ] **Step 4: Verify the row partial works**

Run:
```powershell
curl -s "http://localhost:5099/contacts/1"
```

Expected: HTTP 200; response is a single `<tr id="contact-1">` fragment with the Edit/Delete buttons and `hx-get`/`hx-delete` attributes.

- [ ] **Step 5: Stop the background app**

Stop the `dotnet run` process.

- [ ] **Step 6: Document how to run, then commit**

Add `README.md` at repo root:
```markdown
# HTMX Contacts Demo

Small ASP.NET Core MVC (.NET 10) demo showing six HTMX 2 patterns:
active search, append-on-create, inline edit, swap-back-on-cancel,
in-place update, and delete-row. In-memory data, Tailwind via CDN.

## Run

```powershell
dotnet run --project src/HtmxMvc
```

Then open the URL printed by Kestrel (e.g. http://localhost:5099).

## Layout

- `src/HtmxMvc/Models/Contact.cs` — model
- `src/HtmxMvc/Services/ContactService.cs` — in-memory store (singleton)
- `src/HtmxMvc/Controllers/ContactsController.cs` — page + 6 partial actions
- `src/HtmxMvc/Views/Contacts/Index.cshtml` — single page
- `src/HtmxMvc/Views/Shared/_ContactRow.cshtml` — read-only row
- `src/HtmxMvc/Views/Shared/_ContactEditRow.cshtml` — inline edit row
- `src/HtmxMvc/Views/Shared/_ContactList.cshtml` — tbody for search results
```

Commit:
```powershell
git add README.md
git commit -m "docs: add README with run instructions"
```

---

## Done

After Task 7, the demo is complete and verified. To explore it interactively:

```powershell
dotnet run --project src/HtmxMvc
```

Then in a browser: type in the search box (active search), add a contact (append), click Edit on a row (inline edit) → Save (in-place update) or Cancel (swap-back), click Delete on a row (delete-row).
