using LeoMadeiras.ApiFoundation.Infrastructure.Leomob;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LeoMadeiras.ApiFoundation.Tests;

public class LeomobApiClientFakeTests
{
    private static readonly string[] Links = { "https://arquivos.exemplo.com/1012/sumario.pdf" };

    [Fact]
    public async Task Com_taxa_de_falha_zero_nunca_falha()
    {
        var cliente = CriarCliente(taxaFalha: 0);

        var erro = await Record.ExceptionAsync(() =>
            cliente.NotificarArquivosDisponiveisAsync(1012, 21763699, Links, CancellationToken.None));

        Assert.Null(erro);
    }

    [Fact]
    public async Task Com_taxa_de_falha_um_sempre_falha()
    {
        var cliente = CriarCliente(taxaFalha: 1);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            cliente.NotificarArquivosDisponiveisAsync(1012, 21763699, Links, CancellationToken.None));
    }

    private static LeomobApiClientFake CriarCliente(double taxaFalha) =>
        new(Options.Create(new LeomobFakeOptions { TaxaFalha = taxaFalha }), NullLogger<LeomobApiClientFake>.Instance);
}
