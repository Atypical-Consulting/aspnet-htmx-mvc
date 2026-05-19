# HTMX Contacts Demo — Design

**Date:** 2026-05-19
**Stack:** ASP.NET Core MVC on .NET 10, HTMX 2, Tailwind CSS (Play CDN)

## Goal

A single-page, server-rendered contacts list that demonstrates the six most common HTMX patterns in one cohesive app: active search, append-on-create, inline edit, swap-back-on-cancel, in-place update, and delete-row. No JavaScript beyond HTMX itself.

## Non-goals

- Authentication / authorization
- Persistence beyond process lifetime
- Validation framework beyond MVC defaults (`[Required]`, `ModelState`)
- Tests beyond a smoke check that the app builds and serves the index page
- Production-ready Tailwind build pipeline (Play CDN is acceptable for a demo)

## Project layout

```
HtmxMvc.sln
src/HtmxMvc/
  Program.cs
  HtmxMvc.csproj                    (net10.0, Microsoft.NET.Sdk.Web)
  Controllers/
    ContactsController.cs
  Models/
    Contact.cs
  Services/
    ContactService.cs               (singleton, in-memory, seeded)
  Views/
    _ViewImports.cshtml
    _ViewStart.cshtml
    Shared/
      _Layout.cshtml                (Tailwind + HTMX from CDN)
      _ContactRow.cshtml            (read-only <tr>)
      _ContactEditRow.cshtml        (inline edit <tr>)
      _ContactList.cshtml           (filtered <tbody> rows for search)
    Contacts/
      Index.cshtml                  (search box, add form, table)
  wwwroot/                          (default static files dir, may be empty)
```

The default MVC template's `Home` controller is removed; routing defaults to `Contacts/Index`.

## Data model

```csharp
public sealed record Contact
{
    public int Id { get; init; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
}
```

`Name` is required (validated via `[Required]`); `Email` and `Phone` are free-form strings (no format validation — out of scope).

## ContactService

Singleton, registered via `builder.Services.AddSingleton<ContactService>()`. Thread-safe via a single `lock` over a `List<Contact>` and an `int _nextId`. Seeded in the constructor with 5 contacts.

API:

| Method | Returns | Notes |
|---|---|---|
| `IReadOnlyList<Contact> GetAll()` | Snapshot copy | Sorted by `Id` ascending |
| `IReadOnlyList<Contact> Search(string? q)` | Snapshot copy | Case-insensitive contains over Name/Email/Phone; empty/null `q` returns `GetAll()` |
| `Contact? Get(int id)` | Single | `null` if not found |
| `Contact Add(Contact c)` | The added contact with assigned `Id` | Appends to end |
| `Contact? Update(int id, Contact c)` | The updated contact | `null` if not found |
| `bool Delete(int id)` | `true` if removed | |

## Controller actions

`ContactsController` (route prefix `/contacts` except for `Index` which is `/`):

| Verb | Route | Action | Returns | Purpose |
|---|---|---|---|---|
| GET  | `/`                       | `Index`         | Full page (`Index.cshtml`)         | Initial load |
| GET  | `/contacts/list`          | `List(string? q)` | `_ContactList` partial            | Active search results |
| POST | `/contacts`               | `Create(Contact)` | `_ContactRow` partial             | Append new row to tbody |
| GET  | `/contacts/{id}/edit`     | `Edit(int id)`  | `_ContactEditRow` partial          | Switch row to edit mode |
| PUT  | `/contacts/{id}`          | `Update(int id, Contact)` | `_ContactRow` partial    | Save edits, swap back to read mode |
| GET  | `/contacts/{id}`          | `Row(int id)`   | `_ContactRow` partial              | Cancel edit, restore read row |
| DELETE | `/contacts/{id}`        | `Delete(int id)` | `200 OK` empty body               | Remove row from DOM |

All partial-returning actions use `PartialView(...)`. The `Index` action injects the initial list via `ContactsListViewModel { IReadOnlyList<Contact> Contacts; string Query; }` (or equivalent) so the first paint is server-rendered without an HTMX round-trip.

Validation: `Create` and `Update` check `ModelState.IsValid`; if invalid, return the appropriate partial with the form re-rendered including error messages (`StatusCode 422` so HTMX still swaps).

## HTMX patterns

### 1. Active search
Search input at top of page:
```html
<input type="text" name="q"
       hx-get="/contacts/list"
       hx-trigger="keyup changed delay:300ms, search"
       hx-target="#contact-rows"
       hx-swap="innerHTML"
       placeholder="Search..." />
```

### 2. Append on create
Add form below search:
```html
<form hx-post="/contacts"
      hx-target="#contact-rows"
      hx-swap="afterbegin"
      hx-on::after-request="if(event.detail.successful) this.reset()">
  <input name="Name" required />
  <input name="Email" />
  <input name="Phone" />
  <button>Add</button>
</form>
```

### 3. Inline edit (swap row → edit form row)
In `_ContactRow.cshtml`:
```html
<tr id="contact-@Model.Id">
  <td>@Model.Name</td> ...
  <td>
    <button hx-get="/contacts/@Model.Id/edit"
            hx-target="#contact-@Model.Id"
            hx-swap="outerHTML">Edit</button>
    <button hx-delete="/contacts/@Model.Id"
            hx-target="#contact-@Model.Id"
            hx-swap="outerHTML"
            hx-confirm="Delete @Model.Name?">Delete</button>
  </td>
</tr>
```

### 4. Cancel edit (swap edit row → read row)
In `_ContactEditRow.cshtml`:
```html
<tr id="contact-@Model.Id">
  <td colspan="...">
    <form hx-put="/contacts/@Model.Id"
          hx-target="#contact-@Model.Id"
          hx-swap="outerHTML">
      <input name="Name" value="@Model.Name" required />
      ...
      <button>Save</button>
      <button type="button"
              hx-get="/contacts/@Model.Id"
              hx-target="#contact-@Model.Id"
              hx-swap="outerHTML">Cancel</button>
    </form>
  </td>
</tr>
```

### 5. In-place update
The PUT response is the read-only `_ContactRow` — same `id="contact-@Id"`, swapped via `outerHTML`.

### 6. Delete row
DELETE returns 200 with empty body; `hx-swap="outerHTML"` on the row id removes it from the DOM.

## Layout (`_Layout.cshtml`)

Includes from CDN in `<head>`:
- `https://cdn.tailwindcss.com` (Tailwind Play CDN)
- `https://unpkg.com/htmx.org@2` (HTMX 2, exact version pinned during plan-phase)

Antiforgery: MVC's automatic antiforgery for POST/PUT/DELETE is left on. The HTMX `hx-headers` attribute on the `<body>` injects the antiforgery token from a hidden field rendered by `@Html.AntiForgeryToken()`, e.g.:

```html
<body hx-headers='{"RequestVerificationToken": "@GetAntiforgeryToken()"}'>
```

Where `GetAntiforgeryToken()` is a small helper that pulls the token from `IAntiforgery`. Controller actions are decorated with `[AutoValidateAntiforgeryToken]` at the controller level or `[ValidateAntiForgeryToken]` per action.

## Configuration (`Program.cs`)

- `WebApplication.CreateBuilder(args)`
- `builder.Services.AddControllersWithViews()`
- `builder.Services.AddSingleton<ContactService>()`
- `builder.Services.AddAntiforgery()` (already included by AddControllersWithViews; explicit is fine)
- Default middleware: `UseStaticFiles`, `UseRouting`, `UseAntiforgery`, `MapControllerRoute(default: "{controller=Contacts}/{action=Index}/{id?}")`
- Development: `UseDeveloperExceptionPage`

## Error handling

- 404 for missing contact ids (`Get`, `Edit`, `Update`, `Row`, `Delete` return `NotFound()` when service returns null/false).
- When `ModelState` is invalid in `Create`/`Update`, return `400 Bad Request` with an empty body. Inline validation rendering is explicitly out of scope (see non-goals). The browser already blocks empty `Name` via the `required` attribute, so a 400 is only reachable by clients bypassing the form — the failure mode is "nothing happens", which is acceptable for a demo.

## Testing

A minimal smoke test only:
1. `dotnet build` succeeds.
2. `dotnet run` starts the app; `curl http://localhost:5xxx/` returns 200 and the response body contains the seeded contact names.

No unit tests, no integration test project. (Per non-goals.)

## Open items resolved during plan-phase

- Exact HTMX 2 version to pin in `_Layout.cshtml` (`@2.0.x`).
- Exact `net10.0` `TargetFramework` confirmation (10.0.108 SDK is installed; `net10.0` TFM is correct).
- Final listening port — let Kestrel use the project's `launchSettings.json` default.
