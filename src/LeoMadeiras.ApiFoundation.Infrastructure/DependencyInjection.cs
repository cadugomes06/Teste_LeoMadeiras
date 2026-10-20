using LeoMadeiras.ApiFoundation.Application.Interfaces;
using LeoMadeiras.ApiFoundation.Infrastructure.Armazenamento;
using LeoMadeiras.ApiFoundation.Infrastructure.Leomob;
using LeoMadeiras.ApiFoundation.Infrastructure.Persistence;
using LeoMadeiras.ApiFoundation.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeoMadeiras.ApiFoundation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("ApiFoundation")));
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<INotificacaoLeomobRepository, NotificacaoLeomobRepository>();

        // Fakes sem estado: uma instância serve a aplicação inteira.
        services.AddSingleton<IArmazenamentoArquivos, ArmazenamentoArquivosFake>();
        services.AddOptions<LeomobFakeOptions>()
            .Bind(configuration.GetSection(LeomobFakeOptions.Secao))
            .Validate(o => o.TaxaFalha is >= 0 and <= 1, $"{LeomobFakeOptions.Secao}:TaxaFalha deve estar entre 0 e 1.")
            .ValidateOnStart();
        services.AddSingleton<ILeomobApiClient, LeomobApiClientFake>();

        return services;
    }

    /// <summary>Cria o banco (com o seed dos pedidos) se ainda não existir.</summary>
    public static void InicializarBanco(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }
}
