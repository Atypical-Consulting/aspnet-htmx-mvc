using HtmxMvc.Application.Contacts;
using HtmxMvc.Http;
using Microsoft.AspNetCore.Mvc;

namespace HtmxMvc.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class ContactsController(
    ListContactsHandler list,
    SearchContactsHandler search,
    GetContactHandler get,
    AddContactHandler add,
    UpdateContactHandler update,
    DeleteContactHandler delete) : Controller
{
    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await list.ExecuteAsync(ct));

    [HttpGet("/contacts/list")]
    public async Task<IActionResult> List(string? q, CancellationToken ct)
    {
        var results = await search.ExecuteAsync(q, ct);
        return Request.IsHtmx()
            ? PartialView("_ContactList", results)
            : View("Index", results);
    }

    [HttpPost("/contacts")]
    public async Task<IActionResult> Create(ContactInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        var created = await add.ExecuteAsync(input, ct);
        Response.Htmx(h => h.Trigger("contact-saved", new { id = created.Id, action = "created" }));
        return PartialView("_ContactRow", created);
    }

    [HttpGet("/contacts/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var c = await get.ExecuteAsync(id, ct);
        return c is null ? NotFound() : PartialView("_ContactEditRow", c);
    }

    [HttpPut("/contacts/{id:int}")]
    public async Task<IActionResult> Update(int id, ContactInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        var updated = await update.ExecuteAsync(id, input, ct);
        if (updated is null) return NotFound();
        Response.Htmx(h => h.Trigger("contact-saved", new { id = updated.Id, action = "updated" }));
        return PartialView("_ContactRow", updated);
    }

    [HttpGet("/contacts/{id:int}")]
    public async Task<IActionResult> Row(int id, CancellationToken ct)
    {
        var c = await get.ExecuteAsync(id, ct);
        return c is null ? NotFound() : PartialView("_ContactRow", c);
    }

    [HttpDelete("/contacts/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await delete.ExecuteAsync(id, ct)) return NotFound();
        Response.Htmx(h => h.Trigger("contact-saved", new { id, action = "deleted" }));
        return Ok();
    }
}
