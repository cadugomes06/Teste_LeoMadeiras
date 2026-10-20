using LeoMadeiras.ApiFoundation.Domain.Enums;

namespace LeoMadeiras.ApiFoundation.Domain.Entities;

public class Pedido
{
    public int Id { get; set; }
    public int PedidoSVL { get; set; }
    public long ServiceId { get; set; }
    public TipoPedido TipoPedido { get; set; }

    public List<LinkArquivo> Links { get; set; } = new();
    public List<NotificacaoLeomob> Notificacoes { get; set; } = new();
}
