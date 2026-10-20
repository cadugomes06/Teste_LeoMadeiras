using LeoMadeiras.ApiFoundation.Domain.Enums;

namespace LeoMadeiras.ApiFoundation.Domain.Entities;

public class NotificacaoLeomob
{
    /// <summary>
    /// Status de uma notificação ainda não resolvida. Um pedido tem no máximo uma notificação nesses status
    /// (regra do serviço e índice único no banco usam esta mesma lista).
    /// </summary>
    public static readonly IReadOnlyList<StatusNotificacao> StatusAtivos =
        new[] { StatusNotificacao.INICIAL, StatusNotificacao.PENDENTE };

    public long Id { get; set; }
    public int PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;
    public StatusNotificacao Status { get; set; }
    public int Tentativas { get; set; }
    public string? UltimoErro { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }
}
