namespace HtmxMvc.Domain;

public interface IContactRepository
{
    Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken ct = default);
    Task<Contact?> GetAsync(int id, CancellationToken ct = default);
    Task<Contact> AddAsync(Contact contact, CancellationToken ct = default);
    Task<Contact?> UpdateAsync(int id, Contact contact, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
