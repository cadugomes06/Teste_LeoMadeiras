using LeoMadeiras.ApiFoundation.Application.Interfaces;
using LeoMadeiras.ApiFoundation.Domain.Entities;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeoMadeiras.ApiFoundation.Infrastructure.Persistence.Repositories;

public class NotificacaoLeomobRepository : INotificacaoLeomobRepository
{
    private readonly AppDbContext _contexto;

    public NotificacaoLeomobRepository(AppDbContext contexto) => _contexto = contexto;

    public Task<bool> ExisteAtivaAsync(int pedidoId, CancellationToken cancellationToken) =>
        _contexto.NotificacoesLeomob.AnyAsync(
            n => n.PedidoId == pedidoId && NotificacaoLeomob.StatusAtivos.Contains(n.Status), cancellationToken);

    public Task<List<NotificacaoLeomob>> ListarParaReprocessarAsync(
        DateTime iniciaisAtualizadasAntesDe, CancellationToken cancellationToken) =>
        _contexto.NotificacoesLeomob
            .Include(n => n.Pedido).ThenInclude(p => p.Links)
            // Todas as ativas, exceto as INICIAL recentes (a tentativa delas ainda pode estar em andamento).
            .Where(n => NotificacaoLeomob.StatusAtivos.Contains(n.Status)
                        && (n.Status != StatusNotificacao.INICIAL || n.DataAtualizacao < iniciaisAtualizadasAntesDe))
            .OrderBy(n => n.Id)
            .ToListAsync(cancellationToken);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) =>
        _contexto.SaveChangesAsync(cancellationToken);
}
