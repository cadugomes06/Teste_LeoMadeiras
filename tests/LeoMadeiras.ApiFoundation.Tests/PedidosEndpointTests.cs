using System.Net;
using System.Net.Http.Json;
using LeoMadeiras.ApiFoundation.Application.DTOs;
using LeoMadeiras.ApiFoundation.Domain.Entities;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using LeoMadeiras.ApiFoundation.Tests.Apoio;
using static LeoMadeiras.ApiFoundation.Tests.Apoio.EnvioArquivos;
using Microsoft.AspNetCore.Mvc;

namespace LeoMadeiras.ApiFoundation.Tests;

public class PedidosEndpointTests : IDisposable
{
    private readonly ApiFactory _api = new();
    private readonly HttpClient _cliente;

    public PedidosEndpointTests() => _cliente = _api.CreateClient();

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Pedido_ZMOB_retorna_os_links_recebidos_e_o_status_da_notificacao()
    {
        const string etiqueta = "https://arquivos.exemplo.com/1012/etiqueta-1.png";
        const string usinagem = "https://arquivos.exemplo.com/1012/usinagem.xml";
        var recebimento = await _cliente.PostAsJsonAsync(Rota, Envio(hasErrors: false,
            new { serviceId = 21763699, pedidoSVL = 1012, links = new { etiquetas = new[] { etiqueta }, usinagem = new[] { usinagem } } }));
        recebimento.EnsureSuccessStatusCode();

        var pedido = await ObterAsync(1012);

        Assert.Equal((1012, 21763699L, TipoPedido.ZMOB), (pedido.PedidoSVL, pedido.ServiceId, pedido.TipoPedido));
        Assert.Equal(
            new[] { (CategoriaArquivo.Etiqueta, etiqueta), (CategoriaArquivo.Usinagem, usinagem) },
            pedido.Links.Select(l => (l.Categoria, l.Url)));
        Assert.Equal((StatusNotificacao.CONCLUIDO, 1), (pedido.Notificacao!.Status, pedido.Notificacao.Tentativas));
        Assert.Equal(DateTimeKind.Utc, pedido.Notificacao.DataAtualizacao.Kind); // JSON com "Z"
    }

    [Fact]
    public async Task Pedido_sem_notificacao_retorna_notificacao_nula()
    {
        var pedido = await ObterAsync(1007);

        Assert.Equal(TipoPedido.ZLPL, pedido.TipoPedido);
        Assert.Empty(pedido.Links);
        Assert.Null(pedido.Notificacao);
    }

    [Fact]
    public async Task Com_varias_notificacoes_retorna_a_mais_recente()
    {
        using (var contexto = _api.Banco.CriarContexto())
        {
            contexto.NotificacoesLeomob.AddRange(
                new NotificacaoLeomob { PedidoId = 3, Status = StatusNotificacao.CONCLUIDO, Tentativas = 1 },
                new NotificacaoLeomob { PedidoId = 3, Status = StatusNotificacao.PENDENTE, Tentativas = 1, UltimoErro = "timeout" });
            await contexto.SaveChangesAsync();
        }

        var pedido = await ObterAsync(1012);

        Assert.Equal((StatusNotificacao.PENDENTE, "timeout"), (pedido.Notificacao!.Status, pedido.Notificacao.UltimoErro));
    }

    [Fact]
    public async Task PedidoSVL_que_nao_e_numero_retorna_400()
    {
        var resposta = await _cliente.GetAsync("/api/pedidos/abc");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Pedido_inexistente_retorna_404()
    {
        var resposta = await _cliente.GetAsync("/api/pedidos/9999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>(JsonApi.Opcoes);
        Assert.Contains("9999", problema!.Detail);
    }

    private async Task<PedidoResponse> ObterAsync(int pedidoSVL)
    {
        var resposta = await _cliente.GetAsync($"/api/pedidos/{pedidoSVL}");
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<PedidoResponse>(JsonApi.Opcoes))!;
    }
}
