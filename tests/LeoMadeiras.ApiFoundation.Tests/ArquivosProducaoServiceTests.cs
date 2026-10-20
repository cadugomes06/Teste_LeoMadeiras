using LeoMadeiras.ApiFoundation.Application.DTOs;
using LeoMadeiras.ApiFoundation.Application.Services;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using LeoMadeiras.ApiFoundation.Infrastructure.Persistence;
using LeoMadeiras.ApiFoundation.Infrastructure.Persistence.Repositories;
using LeoMadeiras.ApiFoundation.Tests.Apoio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeoMadeiras.ApiFoundation.Tests;

public class ArquivosProducaoServiceTests : IDisposable
{
    private readonly BancoEmMemoria _banco = new();
    private readonly ArmazenamentoArquivosSpy _armazenamento = new();

    public void Dispose() => _banco.Dispose();

    [Theory]
    [InlineData(1007, 21763653)] // ZLPL
    [InlineData(1008, 21763654)] // ZCVS
    public async Task Pedidos_ZLPL_e_ZCVS_acionam_o_armazenamento_uma_vez_por_arquivo(int pedidoSVL, long serviceId)
    {
        var resultado = await ProcessarAsync(Servico(pedidoSVL, serviceId));

        Assert.Equal(4, resultado.Processados.Single().LinksNovos);
        Assert.Equal(
            new[] { CategoriaArquivo.CortePdfSumario, CategoriaArquivo.CortePdfDetalhado, CategoriaArquivo.Etiqueta, CategoriaArquivo.Usinagem },
            _armazenamento.Copias.Select(c => c.Categoria));
        Assert.All(_armazenamento.Copias, c => Assert.Equal(pedidoSVL, c.PedidoSVL));
    }

    [Fact]
    public async Task Pedido_ZMOB_salva_os_links_mas_nao_aciona_o_armazenamento()
    {
        var resultado = await ProcessarAsync(Servico(1012, 21763699));

        Assert.Equal(4, resultado.Processados.Single().LinksNovos);
        Assert.Equal(4, await ContarLinksAsync(1012));
        Assert.Empty(_armazenamento.Copias);
    }

    [Fact]
    public async Task Envio_repetido_nao_duplica_links_nem_copias()
    {
        await ProcessarAsync(Servico(1007, 21763653));

        var reenvio = await ProcessarAsync(Servico(1007, 21763653));

        Assert.Equal(0, reenvio.Processados.Single().LinksNovos);
        Assert.Equal(4, await ContarLinksAsync(1007));
        Assert.Equal(4, _armazenamento.Copias.Count);
    }

    [Fact]
    public async Task Reenvio_com_um_link_a_mais_grava_e_copia_so_o_novo()
    {
        await ProcessarAsync(Servico(1007, 21763653));
        var servico = Servico(1007, 21763653);
        servico.Links!.Etiquetas!.Add("https://arquivos.exemplo.com/1007/etiqueta-2.png");

        var reenvio = await ProcessarAsync(servico);

        Assert.Equal(1, reenvio.Processados.Single().LinksNovos);
        Assert.Equal("https://arquivos.exemplo.com/1007/etiqueta-2.png", _armazenamento.Copias.Last().Url);
        Assert.Equal(5, _armazenamento.Copias.Count);
    }

    [Fact]
    public async Task Link_repetido_dentro_do_mesmo_envio_e_gravado_uma_unica_vez()
    {
        var servico = Servico(1007, 21763653);
        servico.Links!.Etiquetas!.Add(" https://arquivos.exemplo.com/1007/etiqueta-1.png ");

        var resultado = await ProcessarAsync(servico);

        Assert.Equal(4, resultado.Processados.Single().LinksNovos);
        Assert.Equal(4, await ContarLinksAsync(1007));
    }

    [Fact]
    public async Task Envio_com_um_pedido_inexistente_processa_os_demais_normalmente()
    {
        var resultado = await ProcessarAsync(
            Servico(1007, 21763653),
            Servico(9999, 11111111),
            Servico(1012, 21763699));

        Assert.Equal(new[] { 1007, 1012 }, resultado.Processados.Select(p => p.PedidoSVL));
        var falha = Assert.Single(resultado.Falhas);
        Assert.Equal(9999, falha.PedidoSVL);
        Assert.Contains("não encontrado", falha.Motivo);
        Assert.Equal(4, await ContarLinksAsync(1007));
        Assert.Equal(4, await ContarLinksAsync(1012));
    }

    [Fact]
    public async Task ServiceId_divergente_vira_falha_do_servico_sem_gravar_nada()
    {
        var resultado = await ProcessarAsync(Servico(1007, 21763654));

        Assert.Empty(resultado.Processados);
        Assert.Contains("não corresponde", Assert.Single(resultado.Falhas).Motivo);
        Assert.Equal(0, await ContarLinksAsync(1007));
        Assert.Empty(_armazenamento.Copias);
    }

    [Fact]
    public async Task Falha_na_copia_vira_falha_do_servico_e_o_reenvio_copia_de_novo()
    {
        _armazenamento.FalharParaPedidoSVL = 1007;

        var resultado = await ProcessarAsync(Servico(1007, 21763653), Servico(1008, 21763654));

        Assert.Equal(1007, Assert.Single(resultado.Falhas).PedidoSVL);
        Assert.Equal(1008, Assert.Single(resultado.Processados).PedidoSVL);
        Assert.Equal(0, await ContarLinksAsync(1007));

        _armazenamento.FalharParaPedidoSVL = null;
        var reenvio = await ProcessarAsync(Servico(1007, 21763653));

        Assert.Equal(4, reenvio.Processados.Single().LinksNovos);
        Assert.Equal(4, _armazenamento.Copias.Count(c => c.PedidoSVL == 1007));
    }

    [Fact]
    public async Task Envio_com_hasErrors_true_e_rejeitado_sem_processar_nenhum_servico()
    {
        using var contexto = _banco.CriarContexto();
        var envio = new ArquivosProducaoRequest { HasErrors = true, Services = { Servico(1007, 21763653) } };

        var resultado = await CriarService(contexto).ProcessarAsync(envio, CancellationToken.None);

        Assert.NotNull(resultado.MotivoRejeicao);
        Assert.Null(resultado.Resumo);
        Assert.Equal(0, await ContarLinksAsync(1007));
        Assert.Empty(_armazenamento.Copias);
    }

    /// <summary>Cada chamada usa um contexto novo, como uma requisição HTTP separada.</summary>
    private async Task<ResultadoProcessamento> ProcessarAsync(params ServicoRequest[] servicos)
    {
        using var contexto = _banco.CriarContexto();
        var envio = new ArquivosProducaoRequest { Services = servicos.ToList() };

        var resultado = await CriarService(contexto).ProcessarAsync(envio, CancellationToken.None);
        return resultado.Resumo!;
    }

    private ArquivosProducaoService CriarService(AppDbContext contexto) =>
        new(new PedidoRepository(contexto), _armazenamento, NullLogger<ArquivosProducaoService>.Instance);

    private async Task<int> ContarLinksAsync(int pedidoSVL)
    {
        using var contexto = _banco.CriarContexto();
        return await contexto.Pedidos.Where(p => p.PedidoSVL == pedidoSVL).SelectMany(p => p.Links).CountAsync();
    }

    /// <summary>Serviço no formato do exemplo do enunciado: um arquivo de cada categoria.</summary>
    private static ServicoRequest Servico(int pedidoSVL, long serviceId) => new()
    {
        PedidoSVL = pedidoSVL,
        ServiceId = serviceId,
        Links = new LinksRequest
        {
            Corte = new LinksCorteRequest
            {
                PdfSumario = new() { $"https://arquivos.exemplo.com/{pedidoSVL}/sumario.pdf" },
                PdfDetalhado = new() { $"https://arquivos.exemplo.com/{pedidoSVL}/detalhado.pdf" }
            },
            Etiquetas = new() { $"https://arquivos.exemplo.com/{pedidoSVL}/etiqueta-1.png" },
            Usinagem = new() { $"https://arquivos.exemplo.com/{pedidoSVL}/usinagem.xml" }
        }
    };
}
