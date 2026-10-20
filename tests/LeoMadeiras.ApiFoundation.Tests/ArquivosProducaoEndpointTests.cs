using System.Net;
using System.Net.Http.Json;
using LeoMadeiras.ApiFoundation.Application.DTOs;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using LeoMadeiras.ApiFoundation.Tests.Apoio;
using static LeoMadeiras.ApiFoundation.Tests.Apoio.EnvioArquivos;
using Microsoft.AspNetCore.Mvc;

namespace LeoMadeiras.ApiFoundation.Tests;

public class ArquivosProducaoEndpointTests : IDisposable
{
    private readonly ApiFactory _api = new();
    private readonly HttpClient _cliente;

    public ArquivosProducaoEndpointTests() => _cliente = _api.CreateClient();

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Envio_valido_retorna_200_com_o_resumo_por_servico()
    {
        var resposta = await _cliente.PostAsJsonAsync(Rota, Envio(hasErrors: false,
            ServicoValido(),
            new { serviceId = 1, pedidoSVL = 9999, links = new { } }));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<ResultadoProcessamento>(JsonApi.Opcoes);
        Assert.Equal(new ServicoProcessado(1007, 21763653, TipoPedido.ZLPL, 1, null), Assert.Single(resultado!.Processados));
        Assert.Equal(9999, Assert.Single(resultado.Falhas).PedidoSVL);
    }

    [Fact]
    public async Task Envio_com_hasErrors_true_retorna_422_e_nao_processa_nada()
    {
        var resposta = await _cliente.PostAsJsonAsync(Rota, Envio(hasErrors: true,
            ServicoValido()));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>(JsonApi.Opcoes);
        Assert.Contains("hasErrors", problema!.Detail);

        var pedido = await _cliente.PostAsJsonAsync(Rota, Envio(hasErrors: false,
            ServicoValido()));
        var resultado = await pedido.Content.ReadFromJsonAsync<ResultadoProcessamento>(JsonApi.Opcoes);
        Assert.Equal(1, Assert.Single(resultado!.Processados).LinksNovos); // o envio rejeitado não gravou o link
    }

    [Fact]
    public async Task Servico_sem_serviceId_retorna_400()
    {
        var resposta = await _cliente.PostAsJsonAsync(Rota, Envio(hasErrors: false, new { pedidoSVL = 1007 }));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonApi.Opcoes);
        Assert.Contains(problema!.Errors.Keys, chave => chave.EndsWith("ServiceId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Envio_sem_servicos_retorna_400()
    {
        var resposta = await _cliente.PostAsJsonAsync(Rota, Envio(hasErrors: false));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Servico_nulo_na_lista_retorna_400()
    {
        var resposta = await _cliente.PostAsJsonAsync(Rota, Envio(hasErrors: false, new object?[] { null }));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Theory]
    [InlineData("nao-e-url")]
    [InlineData("ftp://arquivos.exemplo.com/1007/etiqueta-1.png")]
    public async Task Link_que_nao_e_url_http_retorna_400(string url)
    {
        var resposta = await _cliente.PostAsJsonAsync(Rota, Envio(hasErrors: false,
            new { serviceId = 21763653, pedidoSVL = 1007, links = new { etiquetas = new[] { url } } }));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    /// <summary>Pedido 1007 (ZLPL) do seed com uma etiqueta.</summary>
    private static object ServicoValido() =>
        new { serviceId = 21763653, pedidoSVL = 1007, links = new { etiquetas = new[] { "https://arquivos.exemplo.com/1007/etiqueta-1.png" } } };
}
