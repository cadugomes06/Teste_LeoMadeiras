using LeoMadeiras.ApiFoundation.Domain.Entities;

namespace LeoMadeiras.ApiFoundation.Application.Interfaces;

public interface INotificacaoLeomobRepository
{
    /// <summary>Indica se o pedido já tem uma notificação ainda não resolvida (INICIAL ou PENDENTE).</summary>
    Task<bool> ExisteAtivaAsync(int pedidoId, CancellationToken cancellationToken);

    /// <summary>
    /// Notificações a tentar de novo: todas as PENDENTE e as INICIAL paradas desde antes de
    /// <paramref name="iniciaisAtualizadasAntesDe"/>. Vêm com o pedido e os links dele carregados.
    /// </summary>
    Task<List<NotificacaoLeomob>> ListarParaReprocessarAsync(
        DateTime iniciaisAtualizadasAntesDe, CancellationToken cancellationToken);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
