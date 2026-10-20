using LeoMadeiras.ApiFoundation.Domain.Entities;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeoMadeiras.ApiFoundation.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<LinkArquivo> LinksArquivo => Set<LinkArquivo>();
    public DbSet<NotificacaoLeomob> NotificacoesLeomob => Set<NotificacaoLeomob>();

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
            notificacao.HasIndex(n => n.PedidoId)
                .IsUnique()
                .HasFilter($"\"Status\" IN ('{nameof(StatusNotificacao.INICIAL)}', '{nameof(StatusNotificacao.PENDENTE)}')");
        });
    }
}
