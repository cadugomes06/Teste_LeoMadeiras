namespace LeoMadeiras.ApiFoundation.Application.Interfaces;

public interface INotificacaoLeomobRepository
{
    /// <summary>Indica se o pedido já tem uma notificação ainda não resolvida (INICIAL ou PENDENTE).</summary>
    Task<bool> ExisteAtivaAsync(int pedidoId, CancellationToken cancellationToken);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
