using LeoMadeiras.ApiFoundation.Application.Interfaces;
using LeoMadeiras.ApiFoundation.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeoMadeiras.ApiFoundation.Infrastructure.Persistence.Repositories;

public class NotificacaoLeomobRepository : INotificacaoLeomobRepository
{
    private readonly AppDbContext _contexto;

    public NotificacaoLeomobRepository(AppDbContext contexto) => _contexto = contexto;

    public Task<bool> ExisteAtivaAsync(int pedidoId, CancellationToken cancellationToken) =>
        _contexto.NotificacoesLeomob.AnyAsync(
            n => n.PedidoId == pedidoId && NotificacaoLeomob.StatusAtivos.Contains(n.Status), cancellationToken);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) =>
        _contexto.SaveChangesAsync(cancellationToken);
}
