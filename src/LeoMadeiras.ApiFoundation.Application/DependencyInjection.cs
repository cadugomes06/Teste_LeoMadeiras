using LeoMadeiras.ApiFoundation.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LeoMadeiras.ApiFoundation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ArquivosProducaoService>();
        services.AddScoped<NotificacaoLeomobService>();
        services.AddScoped<ConsultaPedidoService>();
        services.AddSingleton<TravaReprocessamento>();

        return services;
    }
}
