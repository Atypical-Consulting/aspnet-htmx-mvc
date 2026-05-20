using HtmxMvc.Domain;

namespace HtmxMvc.Application.Contacts;

public sealed class DeleteContactHandler(IContactRepository repo)
{
    public Task<bool> ExecuteAsync(int id, CancellationToken ct = default)
        => repo.DeleteAsync(id, ct);
}
