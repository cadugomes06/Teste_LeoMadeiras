using LeoMadeiras.ApiFoundation.Domain.Entities;

namespace LeoMadeiras.ApiFoundation.Application.Interfaces;

public interface IPedidoRepository
{
    /// <summary>Busca o pedido junto com os links já recebidos (usados para descartar links repetidos).</summary>
    Task<Pedido?> ObterComLinksAsync(int pedidoSVL, CancellationToken cancellationToken);

    /// <summary>Busca o pedido com links e notificações, só para leitura (alterações nele não são gravadas).</summary>
    Task<Pedido?> ObterParaConsultaAsync(int pedidoSVL, CancellationToken cancellationToken);

    /// <summary>Grava de uma vez (numa única transação) as alterações feitas nos pedidos obtidos.</summary>
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);

    /// <summary>Esquece as alterações ainda não gravadas (usado depois de uma falha, antes de seguir para o próximo serviço).</summary>
    void DescartarAlteracoes();
}
