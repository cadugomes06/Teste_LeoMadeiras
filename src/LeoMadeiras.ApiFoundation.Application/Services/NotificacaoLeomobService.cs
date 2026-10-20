using LeoMadeiras.ApiFoundation.Application.Interfaces;
using LeoMadeiras.ApiFoundation.Domain.Entities;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LeoMadeiras.ApiFoundation.Application.Services;

/// <summary>Envia à LeomobAPI o aviso de arquivos disponíveis e registra o resultado na notificação.</summary>
public class NotificacaoLeomobService
{
    private readonly ILeomobApiClient _leomob;
    private readonly INotificacaoLeomobRepository _notificacoes;
    private readonly ILogger<NotificacaoLeomobService> _logger;

    public NotificacaoLeomobService(
        ILeomobApiClient leomob, INotificacaoLeomobRepository notificacoes, ILogger<NotificacaoLeomobService> logger)
    {
        _leomob = leomob;
        _notificacoes = notificacoes;
        _logger = logger;
    }

    /// <summary>
    /// Faz uma tentativa de envio (regra 6), contada em <see cref="NotificacaoLeomob.Tentativas"/>:
    /// sucesso vira CONCLUIDO; falha vira PENDENTE.
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
            // Qualquer erro significa "não entregue": fica PENDENTE para o reprocessamento tentar de novo.
            notificacao.Status = StatusNotificacao.PENDENTE;
            notificacao.UltimoErro = ex.Message;
            _logger.LogWarning(ex, "Pedido {PedidoSVL}: falha ao notificar a LeomobAPI (tentativa {Tentativas})",
                pedido.PedidoSVL, notificacao.Tentativas);
        }

        notificacao.DataAtualizacao = DateTime.UtcNow;
        await _notificacoes.SalvarAlteracoesAsync(cancellationToken);
    }
}
