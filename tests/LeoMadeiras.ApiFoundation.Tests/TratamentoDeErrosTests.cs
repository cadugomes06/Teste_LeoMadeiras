using System.Net;
using System.Net.Http.Json;
using LeoMadeiras.ApiFoundation.Application.Interfaces;
using LeoMadeiras.ApiFoundation.Domain.Entities;
using LeoMadeiras.ApiFoundation.Tests.Apoio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace LeoMadeiras.ApiFoundation.Tests;

public class TratamentoDeErrosTests
{
    private const string DetalheInterno = "Connection refused (db-interno:5432)";

    [Fact]
    public async Task Erro_inesperado_retorna_500_em_ProblemDetails_sem_vazar_detalhes_internos()
    {
        using var api = new ApiFactory();
        using var apiComBancoFora = api.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.AddScoped<INotificacaoLeomobRepository, RepositorioQueFalha>()));

        var resposta = await apiComBancoFora.CreateClient().PostAsync("/api/notificacoes/reprocessar", content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.DoesNotContain(DetalheInterno, corpo);
        Assert.DoesNotContain(nameof(RepositorioQueFalha), corpo); // nem stack trace
    }

    [Fact]
    public async Task Rota_inexistente_retorna_404_em_ProblemDetails()
    {
        using var api = new ApiFactory();

        var resposta = await api.CreateClient().GetAsync("/api/nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>(JsonApi.Opcoes);
        Assert.Equal(404, problema!.Status);
    }

    /// <summary>Simula o banco fora do ar.</summary>
    private sealed class RepositorioQueFalha : INotificacaoLeomobRepository
    {
        public Task<bool> ExisteAtivaAsync(int pedidoId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(DetalheInterno);

        public Task<List<NotificacaoLeomob>> ListarParaReprocessarAsync(
            DateTime iniciaisAtualizadasAntesDe, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(DetalheInterno);

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException(DetalheInterno);
    }
}
