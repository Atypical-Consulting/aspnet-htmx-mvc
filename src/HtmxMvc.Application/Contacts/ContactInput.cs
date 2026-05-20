using System.ComponentModel.DataAnnotations;

namespace HtmxMvc.Application.Contacts;

public sealed record ContactInput
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    [StringLength(200)]
    public string Email { get; init; } = "";

    [StringLength(50)]
    public string Phone { get; init; } = "";
}
