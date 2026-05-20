using HtmxMvc.Application.Contacts;
using Microsoft.Extensions.DependencyInjection;

namespace HtmxMvc.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ListContactsHandler>();
        services.AddScoped<SearchContactsHandler>();
        services.AddScoped<GetContactHandler>();
        services.AddScoped<AddContactHandler>();
        services.AddScoped<UpdateContactHandler>();
        services.AddScoped<DeleteContactHandler>();
        return services;
    }
}
