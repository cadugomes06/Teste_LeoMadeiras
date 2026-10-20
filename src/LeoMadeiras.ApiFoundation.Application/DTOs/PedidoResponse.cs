using LeoMadeiras.ApiFoundation.Domain.Enums;

namespace LeoMadeiras.ApiFoundation.Application.DTOs;

/// <summary>Pedido com os links recebidos e a situação da notificação à LeomobAPI.</summary>
/// <param name="Notificacao">A notificação mais recente do pedido, ou null se ele nunca foi notificado (ex.: ZLPL/ZCVS).</param>
public record PedidoResponse(
    int PedidoSVL,
    long ServiceId,
    TipoPedido TipoPedido,
    IReadOnlyList<LinkResponse> Links,
    NotificacaoResponse? Notificacao);

public record LinkResponse(CategoriaArquivo Categoria, string Url, DateTime DataRecebimento);

public record NotificacaoResponse(
    StatusNotificacao Status, int Tentativas, string? UltimoErro, DateTime DataCriacao, DateTime DataAtualizacao);
