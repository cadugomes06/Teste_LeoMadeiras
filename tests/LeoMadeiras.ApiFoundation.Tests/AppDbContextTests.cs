using LeoMadeiras.ApiFoundation.Domain.Entities;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using LeoMadeiras.ApiFoundation.Tests.Apoio;
using Microsoft.EntityFrameworkCore;

namespace LeoMadeiras.ApiFoundation.Tests;

public class AppDbContextTests : IDisposable
{
    private const int PedidoIdZlpl = 1;
    private const int PedidoIdZmob = 3;
    private const string UrlEtiqueta = "https://arquivos.exemplo.com/1007/etiqueta-1.png";

    private readonly BancoEmMemoria _banco = new();

    public void Dispose() => _banco.Dispose();

    [Fact]
    public async Task Seed_cria_os_tres_pedidos_iniciais()
    {
        using var contexto = _banco.CriarContexto();

        var pedidos = await contexto.Pedidos.OrderBy(p => p.PedidoSVL).ToListAsync();

        Assert.Collection(pedidos,
            p => Assert.Equal((1007, 21763653L, TipoPedido.ZLPL), (p.PedidoSVL, p.ServiceId, p.TipoPedido)),
            p => Assert.Equal((1008, 21763654L, TipoPedido.ZCVS), (p.PedidoSVL, p.ServiceId, p.TipoPedido)),
            p => Assert.Equal((1012, 21763699L, TipoPedido.ZMOB), (p.PedidoSVL, p.ServiceId, p.TipoPedido)));
    }

    [Fact]
    public async Task Nao_permite_dois_pedidos_com_o_mesmo_PedidoSVL()
    {
        using var contexto = _banco.CriarContexto();
        contexto.Pedidos.Add(new Pedido { PedidoSVL = 1007, ServiceId = 1, TipoPedido = TipoPedido.ZLPL });

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task Nao_permite_gravar_de_novo_um_link_ja_salvo_no_mesmo_pedido()
    {
        await SalvarAsync(NovoLink(CategoriaArquivo.Etiqueta));

        // Simula uma segunda requisição: outro contexto tentando gravar o mesmo link.
        await Assert.ThrowsAsync<DbUpdateException>(() => SalvarAsync(NovoLink(CategoriaArquivo.Etiqueta)));
    }

    [Fact]
    public async Task Permite_a_mesma_url_em_outra_categoria()
    {
        await SalvarAsync(NovoLink(CategoriaArquivo.Etiqueta));

        await SalvarAsync(NovoLink(CategoriaArquivo.Usinagem));
    }

    [Fact]
    public async Task Nao_permite_duas_notificacoes_ativas_para_o_mesmo_pedido()
    {
        await SalvarAsync(NovaNotificacao(StatusNotificacao.PENDENTE));

        await Assert.ThrowsAsync<DbUpdateException>(() => SalvarAsync(NovaNotificacao(StatusNotificacao.INICIAL)));
    }

    [Fact]
    public async Task Permite_nova_notificacao_quando_a_anterior_ja_foi_resolvida()
    {
        await SalvarAsync(NovaNotificacao(StatusNotificacao.CONCLUIDO));

        await SalvarAsync(NovaNotificacao(StatusNotificacao.INICIAL));
    }

    private async Task SalvarAsync(object entidade)
    {
        using var contexto = _banco.CriarContexto();
        contexto.Add(entidade);
        await contexto.SaveChangesAsync();
    }

    private static LinkArquivo NovoLink(CategoriaArquivo categoria) => new()
    {
        PedidoId = PedidoIdZlpl,
        Categoria = categoria,
        Url = UrlEtiqueta,
        DataRecebimento = DateTime.UtcNow
    };

    private static NotificacaoLeomob NovaNotificacao(StatusNotificacao status) => new()
    {
        PedidoId = PedidoIdZmob,
        Status = status,
        DataCriacao = DateTime.UtcNow,
        DataAtualizacao = DateTime.UtcNow
    };
}
