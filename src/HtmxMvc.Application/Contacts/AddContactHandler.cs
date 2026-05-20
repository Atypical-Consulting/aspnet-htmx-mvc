using HtmxMvc.Domain;

namespace HtmxMvc.Application.Contacts;

public sealed class AddContactHandler(IContactRepository repo)
{
    public Task<Contact> ExecuteAsync(ContactInput input, CancellationToken ct = default)
    {
        var contact = new Contact
        {
            Name = input.Name,
            Email = input.Email,
            Phone = input.Phone
        };
        return repo.AddAsync(contact, ct);
    }
}
