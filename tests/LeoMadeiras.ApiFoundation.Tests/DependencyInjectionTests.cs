using LeoMadeiras.ApiFoundation.Application.Interfaces;
using LeoMadeiras.ApiFoundation.Infrastructure;
using LeoMadeiras.ApiFoundation.Infrastructure.Armazenamento;
using LeoMadeiras.ApiFoundation.Infrastructure.Leomob;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LeoMadeiras.ApiFoundation.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_registra_os_fakes_e_le_a_taxa_de_falha_da_configuracao()
    {
        using var provider = CriarProvider(taxaFalha: "0.25");

        Assert.IsType<ArmazenamentoArquivosFake>(provider.GetRequiredService<IArmazenamentoArquivos>());
        Assert.IsType<LeomobApiClientFake>(provider.GetRequiredService<ILeomobApiClient>());
        Assert.Equal(0.25, provider.GetRequiredService<IOptions<LeomobFakeOptions>>().Value.TaxaFalha);
    }

    [Theory]
    [InlineData("-0.1")]
    [InlineData("50")]
    public void Recusa_taxa_de_falha_fora_do_intervalo_de_0_a_1(string taxaFalha)
    {
        using var provider = CriarProvider(taxaFalha);

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<LeomobFakeOptions>>().Value);
    }

    private static ServiceProvider CriarProvider(string taxaFalha)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["LeomobFake:TaxaFalha"] = taxaFalha })
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddInfrastructure(configuracao)
            .BuildServiceProvider();
    }
}
