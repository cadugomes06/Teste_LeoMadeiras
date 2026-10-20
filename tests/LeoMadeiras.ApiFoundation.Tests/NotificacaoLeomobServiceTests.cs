using LeoMadeiras.ApiFoundation.Application.DTOs;
using LeoMadeiras.ApiFoundation.Application.Services;
using LeoMadeiras.ApiFoundation.Domain.Entities;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using LeoMadeiras.ApiFoundation.Infrastructure.Persistence.Repositories;
using LeoMadeiras.ApiFoundation.Tests.Apoio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeoMadeiras.ApiFoundation.Tests;

public class NotificacaoLeomobServiceTests : IDisposable
{
    // Só pedidos ZMOB têm notificação; este é o do seed (1012).
    private const int PedidoZmob = 3;

    private readonly BancoEmMemoria _banco = new();
    private readonly LeomobApiClientSpy _leomob = new();

    public void Dispose() => _banco.Dispose();

    [Fact]
    public async Task Reprocessamento_conclui_a_notificacao_pendente_quando_a_Leomob_responde()
    {
        var id = await CriarNotificacaoAsync(StatusNotificacao.PENDENTE, tentativas: 1);

        var resultado = await ReprocessarAsync();

        Assert.Equal(new ResultadoReprocessamento(1, 1, 0, 0), resultado);
        var notificacao = await ObterAsync(id);
        Assert.Equal((StatusNotificacao.CONCLUIDO, 2), (notificacao.Status, notificacao.Tentativas));
        Assert.Null(notificacao.UltimoErro);
    }

    [Fact]
    public async Task Segunda_tentativa_sem_sucesso_continua_PENDENTE()
    {
        var id = await CriarNotificacaoAsync(StatusNotificacao.PENDENTE, tentativas: 1);
        _leomob.Falhar = true;

        var resultado = await ReprocessarAsync();

        Assert.Equal(new ResultadoReprocessamento(1, 0, 1, 0), resultado);
        var notificacao = await ObterAsync(id);
        Assert.Equal((StatusNotificacao.PENDENTE, 2), (notificacao.Status, notificacao.Tentativas));
    }

    [Fact]
    public async Task Notificacao_vai_para_FALHA_apos_a_terceira_tentativa_sem_sucesso_e_nao_e_mais_tentada()
    {
        var id = await CriarNotificacaoAsync(StatusNotificacao.PENDENTE, tentativas: 2);
        _leomob.Falhar = true;

        var terceiraTentativa = await ReprocessarAsync();
        var depoisDaFalha = await ReprocessarAsync();

        Assert.Equal(new ResultadoReprocessamento(1, 0, 0, 1), terceiraTentativa);
        Assert.Equal(new ResultadoReprocessamento(0, 0, 0, 0), depoisDaFalha);
        var notificacao = await ObterAsync(id);
        Assert.Equal((StatusNotificacao.FALHA, 3), (notificacao.Status, notificacao.Tentativas));
        Assert.Single(_leomob.Chamadas);
    }

    [Fact]
    public async Task INICIAL_parada_ha_mais_tempo_que_o_limite_e_reprocessada()
    {
        var id = await CriarNotificacaoAsync(StatusNotificacao.INICIAL, tentativas: 0,
            atualizadaHa: NotificacaoLeomobService.TempoMaximoEmInicial + TimeSpan.FromMinutes(1));

        var resultado = await ReprocessarAsync();

        Assert.Equal(new ResultadoReprocessamento(1, 1, 0, 0), resultado);
        var notificacao = await ObterAsync(id);
        Assert.Equal((StatusNotificacao.CONCLUIDO, 1), (notificacao.Status, notificacao.Tentativas));
    }

    [Fact]
    public async Task INICIAL_recente_nao_e_reprocessada_porque_a_tentativa_pode_estar_em_andamento()
    {
        var id = await CriarNotificacaoAsync(StatusNotificacao.INICIAL, tentativas: 0,
            atualizadaHa: NotificacaoLeomobService.TempoMaximoEmInicial - TimeSpan.FromMinutes(1));

        var resultado = await ReprocessarAsync();

        Assert.Equal(0, resultado.Reprocessadas);
        Assert.Equal(StatusNotificacao.INICIAL, (await ObterAsync(id)).Status);
    }

    [Fact]
    public async Task Notificacoes_CONCLUIDO_e_FALHA_nao_sao_reprocessadas()
    {
        await CriarNotificacaoAsync(StatusNotificacao.CONCLUIDO, tentativas: 1);
        await CriarNotificacaoAsync(StatusNotificacao.FALHA, tentativas: 3);

        var resultado = await ReprocessarAsync();

        Assert.Equal(0, resultado.Reprocessadas);
        Assert.Empty(_leomob.Chamadas);
    }

    [Fact]
    public async Task Retentativa_envia_todos_os_links_do_pedido()
    {
        using (var contexto = _banco.CriarContexto())
        {
            contexto.LinksArquivo.AddRange(
                new LinkArquivo { PedidoId = PedidoZmob, Categoria = CategoriaArquivo.Etiqueta, Url = "https://arquivos.exemplo.com/1012/etiqueta-1.png" },
                new LinkArquivo { PedidoId = PedidoZmob, Categoria = CategoriaArquivo.Usinagem, Url = "https://arquivos.exemplo.com/1012/usinagem.xml" });
            await contexto.SaveChangesAsync();
        }
        await CriarNotificacaoAsync(StatusNotificacao.PENDENTE, tentativas: 1);

        await ReprocessarAsync();

        Assert.Equal(2, Assert.Single(_leomob.Chamadas).Count);
    }

    /// <summary>Cada rodada usa um contexto novo, como uma requisição HTTP separada.</summary>
    private async Task<ResultadoReprocessamento> ReprocessarAsync()
    {
        using var contexto = _banco.CriarContexto();
        var service = new NotificacaoLeomobService(
            _leomob, new NotificacaoLeomobRepository(contexto), new TravaReprocessamento(), NullLogger<NotificacaoLeomobService>.Instance);

        return await service.ReprocessarAsync(CancellationToken.None);
    }

    private async Task<long> CriarNotificacaoAsync(StatusNotificacao status, int tentativas, TimeSpan atualizadaHa = default)
    {
        using var contexto = _banco.CriarContexto();
        var data = DateTime.UtcNow - atualizadaHa;
        var notificacao = new NotificacaoLeomob
        {
            PedidoId = PedidoZmob,
            Status = status,
            Tentativas = tentativas,
            DataCriacao = data,
            DataAtualizacao = data
        };
        contexto.NotificacoesLeomob.Add(notificacao);
        await contexto.SaveChangesAsync();
        return notificacao.Id;
    }

    private async Task<NotificacaoLeomob> ObterAsync(long id)
    {
        using var contexto = _banco.CriarContexto();
        return await contexto.NotificacoesLeomob.SingleAsync(n => n.Id == id);
    }
}
