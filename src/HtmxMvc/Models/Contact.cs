using System.ComponentModel.DataAnnotations;

namespace HtmxMvc.Models;

public sealed class Contact
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(200)]
    public string Email { get; set; } = "";

    [StringLength(50)]
    public string Phone { get; set; } = "";
}
