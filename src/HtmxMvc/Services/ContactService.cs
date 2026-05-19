using HtmxMvc.Models;

namespace HtmxMvc.Services;

public sealed class ContactService
{
    private readonly object _gate = new();
    private readonly List<Contact> _contacts = new();
    private int _nextId = 1;

    public ContactService()
    {
        Add(new Contact { Name = "Ada Lovelace",      Email = "ada@analyticalengine.org",  Phone = "555-0101" });
        Add(new Contact { Name = "Alan Turing",       Email = "alan@bletchley.uk",         Phone = "555-0102" });
        Add(new Contact { Name = "Grace Hopper",      Email = "grace@navy.mil",            Phone = "555-0103" });
        Add(new Contact { Name = "Edsger Dijkstra",   Email = "edsger@eindhoven.nl",       Phone = "555-0104" });
        Add(new Contact { Name = "Margaret Hamilton", Email = "margaret@apollo.nasa.gov",  Phone = "555-0105" });
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
