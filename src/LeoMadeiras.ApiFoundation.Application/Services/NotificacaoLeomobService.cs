using LeoMadeiras.ApiFoundation.Application.DTOs;
using LeoMadeiras.ApiFoundation.Application.Interfaces;
using LeoMadeiras.ApiFoundation.Domain.Entities;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LeoMadeiras.ApiFoundation.Application.Services;

/// <summary>Envia à LeomobAPI o aviso de arquivos disponíveis e registra o resultado na notificação.</summary>
public class NotificacaoLeomobService
{
    /// <summary>Tentativas sem sucesso (contando a primeira chamada) até a notificação ir para FALHA.</summary>
    public const int MaximoTentativas = 3;

    /// <summary>
    /// Uma notificação INICIAL só fica parada se o processo caiu no meio da tentativa;
    /// depois desse tempo o reprocessamento assume que ela não vai terminar sozinha.
    /// </summary>
    public static readonly TimeSpan TempoMaximoEmInicial = TimeSpan.FromMinutes(5);

    private readonly ILeomobApiClient _leomob;
    private readonly INotificacaoLeomobRepository _notificacoes;
    private readonly TravaReprocessamento _trava;
    private readonly ILogger<NotificacaoLeomobService> _logger;

    public NotificacaoLeomobService(
        ILeomobApiClient leomob,
        INotificacaoLeomobRepository notificacoes,
        TravaReprocessamento trava,
        ILogger<NotificacaoLeomobService> logger)
    {
        _leomob = leomob;
        _notificacoes = notificacoes;
        _trava = trava;
        _logger = logger;
    }

    /// <summary>
    /// Faz uma tentativa de envio (regra 6), contada em <see cref="NotificacaoLeomob.Tentativas"/>:
    /// sucesso vira CONCLUIDO; falha vira PENDENTE, ou FALHA ao chegar a <see cref="MaximoTentativas"/>.
    /// A notificação precisa vir com o pedido e os links dele carregados, pois todos os links são enviados.
    /// </summary>
    public async Task TentarEnviarAsync(NotificacaoLeomob notificacao, CancellationToken cancellationToken)
    {
        var pedido = notificacao.Pedido;
        var links = pedido.Links.Select(l => l.Url).ToList();
        notificacao.Tentativas++;

        try
        {
            await _leomob.NotificarArquivosDisponiveisAsync(pedido.PedidoSVL, pedido.ServiceId, links, cancellationToken);

            notificacao.Status = StatusNotificacao.CONCLUIDO;
            notificacao.UltimoErro = null;
            _logger.LogInformation("Pedido {PedidoSVL}: LeomobAPI notificada", pedido.PedidoSVL);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Qualquer erro significa "não entregue": tenta de novo depois, até o limite de tentativas.
            notificacao.Status = notificacao.Tentativas >= MaximoTentativas
                ? StatusNotificacao.FALHA
                : StatusNotificacao.PENDENTE;
            notificacao.UltimoErro = ex.Message;
            _logger.LogWarning(ex, "Pedido {PedidoSVL}: falha ao notificar a LeomobAPI (tentativa {Tentativas}), status {Status}",
                pedido.PedidoSVL, notificacao.Tentativas, notificacao.Status);
        }

        notificacao.DataAtualizacao = DateTime.UtcNow;
        await _notificacoes.SalvarAlteracoesAsync(cancellationToken);
    }

    /// <summary>
    /// Tenta de novo as notificações PENDENTE (e as INICIAL paradas). Notificações em FALHA ou CONCLUIDO não são
    /// mais tentadas. Usado pelo worker de reprocessamento automático e pelo endpoint manual, uma rodada por vez.
    /// </summary>
    public async Task<ResultadoReprocessamento> ReprocessarAsync(CancellationToken cancellationToken)
    {
        await _trava.EsperarAsync(cancellationToken);
        try
        {
            return await ReprocessarPendentesAsync(cancellationToken);
        }
        finally
        {
            _trava.Liberar();
        }
    }

    private async Task<ResultadoReprocessamento> ReprocessarPendentesAsync(CancellationToken cancellationToken)
    {
        var notificacoes = await _notificacoes.ListarParaReprocessarAsync(
            DateTime.UtcNow - TempoMaximoEmInicial, cancellationToken);

        foreach (var notificacao in notificacoes)
        {
            // Se o cliente desistir, não começa a próxima; mas a tentativa já iniciada vai até o fim,
            // senão a LeomobAPI poderia ser avisada sem o resultado ficar gravado (e seria avisada de novo).
            cancellationToken.ThrowIfCancellationRequested();
            using var escopoLog = _logger.BeginScope("PedidoSVL {PedidoSVL}", notificacao.Pedido.PedidoSVL);
            await TentarEnviarAsync(notificacao, CancellationToken.None);
        }

        var resultado = new ResultadoReprocessamento(
            notificacoes.Count,
            notificacoes.Count(n => n.Status == StatusNotificacao.CONCLUIDO),
            notificacoes.Count(n => n.Status == StatusNotificacao.PENDENTE),
            notificacoes.Count(n => n.Status == StatusNotificacao.FALHA));

        _logger.LogInformation(
            "Reprocessamento: {Reprocessadas} notificações ({Concluidas} concluídas, {Pendentes} pendentes, {Falhas} em falha)",
            resultado.Reprocessadas, resultado.Concluidas, resultado.Pendentes, resultado.Falhas);
        return resultado;
    }
}
