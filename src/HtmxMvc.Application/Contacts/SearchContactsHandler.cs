using HtmxMvc.Domain;

namespace HtmxMvc.Application.Contacts;

public sealed class SearchContactsHandler(IContactRepository repo)
{
    public async Task<IReadOnlyList<Contact>> ExecuteAsync(string? q, CancellationToken ct = default)
    {
        var all = await repo.GetAllAsync(ct);
        if (string.IsNullOrWhiteSpace(q)) return all;
        return all.Where(c =>
            c.Name.Contains(q,  StringComparison.OrdinalIgnoreCase) ||
            c.Email.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            c.Phone.Contains(q, StringComparison.OrdinalIgnoreCase))
        .ToList();
    }
}
