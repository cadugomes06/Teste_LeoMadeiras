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
    private readonly string? _connectionString;
    private readonly bool _reprocessamentoAutomatico;

    /// <param name="taxaFalhaLeomob">0 = a LeomobAPI falsa nunca falha; 1 = sempre falha.</param>
    /// <param name="connectionString">Banco a usar no lugar do <see cref="Banco"/> em memória (ex.: um arquivo temporário).</param>
    /// <param name="reprocessamentoAutomatico">Liga o worker, rodando a cada 1 segundo.</param>
    public ApiFactory(double taxaFalhaLeomob = 0, string? connectionString = null, bool reprocessamentoAutomatico = false)
    {
        _taxaFalhaLeomob = taxaFalhaLeomob;
        _connectionString = connectionString;
        _reprocessamentoAutomatico = reprocessamentoAutomatico;
    }

    public BancoEmMemoria Banco { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Fixa o comportamento da LeomobAPI falsa e do worker, independentemente do que estiver nos appsettings.
        // Por padrão o worker fica desligado: os testes controlam quando reprocessar.
        builder.ConfigureAppConfiguration(configuracao => configuracao.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["LeomobFake:TaxaFalha"] = _taxaFalhaLeomob.ToString(CultureInfo.InvariantCulture),
                ["ReprocessamentoAutomatico:Habilitado"] = _reprocessamentoAutomatico.ToString(),
                ["ReprocessamentoAutomatico:IntervaloSegundos"] = "1"
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                if (_connectionString is null)
                    options.UseSqlite(Banco.Conexao);
                else
                    options.UseSqlite(_connectionString);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            Banco.Dispose();
    }
}
