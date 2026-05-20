using HtmxMvc.Domain;

namespace HtmxMvc.Application.Contacts;

public sealed class GetContactHandler(IContactRepository repo)
{
    public Task<Contact?> ExecuteAsync(int id, CancellationToken ct = default)
        => repo.GetAsync(id, ct);
}
