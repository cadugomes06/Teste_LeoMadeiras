using LeoMadeiras.ApiFoundation.Domain.Enums;

namespace LeoMadeiras.ApiFoundation.Domain.Entities;

public class NotificacaoLeomob
{
    public long Id { get; set; }
    public int PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;
    public StatusNotificacao Status { get; set; }
    public int Tentativas { get; set; }
    public string? UltimoErro { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }
}
