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
    private readonly BancoEmMemoria _banco = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // A LeomobAPI falsa nunca falha aqui, independentemente do que estiver nos appsettings.
        builder.ConfigureAppConfiguration(configuracao => configuracao.AddInMemoryCollection(
            new Dictionary<string, string?> { ["LeomobFake:TaxaFalha"] = "0" }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_banco.Conexao));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _banco.Dispose();
    }
}
