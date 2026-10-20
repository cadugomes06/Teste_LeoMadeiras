using LeoMadeiras.ApiFoundation.Application.Interfaces;
using LeoMadeiras.ApiFoundation.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeoMadeiras.ApiFoundation.Infrastructure.Persistence.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly AppDbContext _contexto;

    public PedidoRepository(AppDbContext contexto) => _contexto = contexto;

    public Task<Pedido?> ObterComLinksAsync(int pedidoSVL, CancellationToken cancellationToken) =>
        _contexto.Pedidos
            .Include(p => p.Links)
            .FirstOrDefaultAsync(p => p.PedidoSVL == pedidoSVL, cancellationToken);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) =>
        _contexto.SaveChangesAsync(cancellationToken);

    public void DescartarAlteracoes() => _contexto.ChangeTracker.Clear();
}
