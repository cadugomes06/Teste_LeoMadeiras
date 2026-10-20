using LeoMadeiras.ApiFoundation.Application.DTOs;
using LeoMadeiras.ApiFoundation.Application.Interfaces;

namespace LeoMadeiras.ApiFoundation.Application.Services;

/// <summary>Monta a visão de um pedido para consulta: dados, links recebidos e status da notificação.</summary>
public class ConsultaPedidoService
{
    private readonly IPedidoRepository _pedidos;

    public ConsultaPedidoService(IPedidoRepository pedidos) => _pedidos = pedidos;

    /// <returns>O pedido, ou null se não existir.</returns>
    public async Task<PedidoResponse?> ObterAsync(int pedidoSVL, CancellationToken cancellationToken)
    {
        var pedido = await _pedidos.ObterParaConsultaAsync(pedidoSVL, cancellationToken);
        if (pedido is null)
            return null;

        var links = pedido.Links
            .OrderBy(l => l.Categoria).ThenBy(l => l.DataRecebimento).ThenBy(l => l.Id)
            .Select(l => new LinkResponse(l.Categoria, l.Url, l.DataRecebimento))
            .ToList();

        // Um pedido pode ter várias notificações ao longo do tempo (uma nova a cada lote de links); vale a última
        // criada (o Id é autoincremento, então segue a ordem de criação).
        var ultima = pedido.Notificacoes.MaxBy(n => n.Id);
        var notificacao = ultima is null
            ? null
            : new NotificacaoResponse(ultima.Status, ultima.Tentativas, ultima.UltimoErro, ultima.DataCriacao, ultima.DataAtualizacao);

        return new PedidoResponse(
            pedido.PedidoSVL,
            pedido.ServiceId,
            pedido.TipoPedido,
            links,
            notificacao);
    }
}
