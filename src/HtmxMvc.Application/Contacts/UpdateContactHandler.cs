using HtmxMvc.Domain;

namespace HtmxMvc.Application.Contacts;

public sealed class UpdateContactHandler(IContactRepository repo)
{
    public Task<Contact?> ExecuteAsync(int id, ContactInput input, CancellationToken ct = default)
    {
        var contact = new Contact
        {
            Name = input.Name,
            Email = input.Email,
            Phone = input.Phone
        };
        return repo.UpdateAsync(id, contact, ct);
    }
}
