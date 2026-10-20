using System.Globalization;
using LeoMadeiras.ApiFoundation.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LeoMadeiras.ApiFoundation.Tests.Apoio;

/// <summary>Sobe a API de verdade em memória, trocando o banco em arquivo por um SQLite em memória.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly double _taxaFalhaLeomob;

    /// <param name="taxaFalhaLeomob">0 = a LeomobAPI falsa nunca falha; 1 = sempre falha.</param>
    public ApiFactory(double taxaFalhaLeomob = 0) => _taxaFalhaLeomob = taxaFalhaLeomob;

    public BancoEmMemoria Banco { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Fixa o comportamento da LeomobAPI falsa, independentemente do que estiver nos appsettings.
        builder.ConfigureAppConfiguration(configuracao => configuracao.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["LeomobFake:TaxaFalha"] = _taxaFalhaLeomob.ToString(CultureInfo.InvariantCulture)
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(Banco.Conexao));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            Banco.Dispose();
    }
}
