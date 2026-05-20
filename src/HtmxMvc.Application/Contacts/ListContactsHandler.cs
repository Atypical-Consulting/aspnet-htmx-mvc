using HtmxMvc.Domain;

namespace HtmxMvc.Application.Contacts;

public sealed class ListContactsHandler(IContactRepository repo)
{
    public Task<IReadOnlyList<Contact>> ExecuteAsync(CancellationToken ct = default)
        => repo.GetAllAsync(ct);
}
