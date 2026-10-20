using System.Net.Http.Json;
using LeoMadeiras.ApiFoundation.Application.DTOs;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using LeoMadeiras.ApiFoundation.Tests.Apoio;
using Microsoft.EntityFrameworkCore;

namespace LeoMadeiras.ApiFoundation.Tests;

public class NotificacoesEndpointTests : IDisposable
{
    // LeomobAPI falsa sempre fora do ar: toda tentativa falha.
    private readonly ApiFactory _api = new(taxaFalhaLeomob: 1);
    private readonly HttpClient _cliente;

    public NotificacoesEndpointTests() => _cliente = _api.CreateClient();

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Notificacao_ZMOB_vai_para_FALHA_apos_a_terceira_tentativa_sem_sucesso()
    {
        // Tentativa 1: a chamada feita no recebimento dos arquivos.
        var recebimento = await _cliente.PostAsJsonAsync("/api/arquivos-producao", new
        {
            documentType = "filesToProduction",
            hasErrors = false,
            services = new[]
            {
                new { serviceId = 21763699, pedidoSVL = 1012, links = new { etiquetas = new[] { "https://arquivos.exemplo.com/1012/etiqueta-1.png" } } }
            }
        });
        var resumo = await recebimento.Content.ReadFromJsonAsync<ResultadoProcessamento>(JsonApi.Opcoes);
        Assert.Equal(StatusNotificacao.PENDENTE, Assert.Single(resumo!.Processados).Notificacao);

        // Tentativas 2 e 3: reprocessamentos. Depois disso a notificação não é mais tentada.
        Assert.Equal(new ResultadoReprocessamento(1, 0, 1, 0), await ReprocessarAsync());
        Assert.Equal(new ResultadoReprocessamento(1, 0, 0, 1), await ReprocessarAsync());
        Assert.Equal(new ResultadoReprocessamento(0, 0, 0, 0), await ReprocessarAsync());

        using var contexto = _api.Banco.CriarContexto();
        var notificacao = await contexto.NotificacoesLeomob.SingleAsync();
        Assert.Equal((StatusNotificacao.FALHA, 3), (notificacao.Status, notificacao.Tentativas));
    }

    private async Task<ResultadoReprocessamento> ReprocessarAsync()
    {
        var resposta = await _cliente.PostAsync("/api/notificacoes/reprocessar", content: null);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<ResultadoReprocessamento>(JsonApi.Opcoes))!;
    }
}
