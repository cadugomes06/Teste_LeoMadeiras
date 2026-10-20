using LeoMadeiras.ApiFoundation.Application.Exceptions;
using LeoMadeiras.ApiFoundation.Domain.Entities;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LeoMadeiras.ApiFoundation.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<LinkArquivo> LinksArquivo => Set<LinkArquivo>();
    public DbSet<NotificacaoLeomob> NotificacoesLeomob => Set<NotificacaoLeomob>();

    /// <summary>
    /// Traduz a violação de índice único do SQLite para uma exceção da Application, que não conhece o EF.
    /// (SaveChangesAsync(CancellationToken) também passa por aqui.)
    /// </summary>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraint })
        {
            throw new RegistroDuplicadoException("O registro já foi gravado por outro processamento.", ex);
        }
    }

    // SQLITE_CONSTRAINT_UNIQUE: https://www.sqlite.org/rescode.html#constraint_unique
    private const int SqliteUniqueConstraint = 2067;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pedido>(pedido =>
        {
            pedido.HasIndex(p => p.PedidoSVL).IsUnique();
            pedido.Property(p => p.TipoPedido).HasConversion<string>().HasMaxLength(4);
            pedido.HasMany(p => p.Links).WithOne().HasForeignKey(l => l.PedidoId);
            pedido.HasMany(p => p.Notificacoes).WithOne(n => n.Pedido).HasForeignKey(n => n.PedidoId);

            pedido.HasData(
                new Pedido { Id = 1, PedidoSVL = 1007, ServiceId = 21763653, TipoPedido = TipoPedido.ZLPL },
                new Pedido { Id = 2, PedidoSVL = 1008, ServiceId = 21763654, TipoPedido = TipoPedido.ZCVS },
                new Pedido { Id = 3, PedidoSVL = 1012, ServiceId = 21763699, TipoPedido = TipoPedido.ZMOB });
        });

        modelBuilder.Entity<LinkArquivo>(link =>
        {
            // Base da idempotência: o mesmo link não pode ser gravado duas vezes para o mesmo pedido.
            link.HasIndex(l => new { l.PedidoId, l.Categoria, l.Url }).IsUnique();
            link.Property(l => l.Categoria).HasConversion<string>().HasMaxLength(30);
            link.Property(l => l.Url).HasMaxLength(LinkArquivo.TamanhoMaximoUrl);
        });

        modelBuilder.Entity<NotificacaoLeomob>(notificacao =>
        {
            notificacao.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);
            notificacao.HasIndex(n => n.Status);

            // No máximo uma notificação ativa (ainda não resolvida) por pedido, mesmo com requisições concorrentes.
            // Gera o filtro SQL: "Status" IN ('INICIAL', 'PENDENTE')
            notificacao.HasIndex(n => n.PedidoId)
                .IsUnique()
                .HasFilter($"\"Status\" IN ({string.Join(", ", NotificacaoLeomob.StatusAtivos.Select(s => $"'{s}'"))})");
        });
    }
}
