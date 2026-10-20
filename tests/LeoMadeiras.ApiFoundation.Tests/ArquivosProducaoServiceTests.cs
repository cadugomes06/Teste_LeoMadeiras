using LeoMadeiras.ApiFoundation.Application.DTOs;
using LeoMadeiras.ApiFoundation.Application.Services;
using LeoMadeiras.ApiFoundation.Domain.Entities;
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
    private readonly LeomobApiClientSpy _leomob = new();

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
        var reenvio = await ProcessarAsync(ServicoComEtiquetaExtra(1007, 21763653));

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
        _armazenamento.AoCopiar = pedidoSVL =>
        {
            if (pedidoSVL == 1007) throw new IOException("Armazenamento indisponível (falha simulada).");
        };

        var resultado = await ProcessarAsync(Servico(1007, 21763653), Servico(1008, 21763654));

        Assert.Equal(1007, Assert.Single(resultado.Falhas).PedidoSVL);
        Assert.Equal(1008, Assert.Single(resultado.Processados).PedidoSVL);
        Assert.Equal(0, await ContarLinksAsync(1007));

        _armazenamento.AoCopiar = null;
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

    [Fact]
    public async Task Pedido_ZMOB_grava_INICIAL_antes_de_chamar_a_Leomob_e_conclui_quando_ela_responde()
    {
        StatusNotificacao? statusDuranteAChamada = null;
        _leomob.AoNotificar = () =>
        {
            using var outroContexto = _banco.CriarContexto();
            statusDuranteAChamada = outroContexto.NotificacoesLeomob.Single().Status;
        };

        var resultado = await ProcessarAsync(Servico(1012, 21763699));

        Assert.Equal(StatusNotificacao.INICIAL, statusDuranteAChamada);
        Assert.Equal(StatusNotificacao.CONCLUIDO, resultado.Processados.Single().Notificacao);
        var notificacao = Assert.Single(await ObterNotificacoesAsync(1012));
        Assert.Equal((StatusNotificacao.CONCLUIDO, 1), (notificacao.Status, notificacao.Tentativas));
        Assert.Equal(4, Assert.Single(_leomob.Chamadas).Count);
    }

    [Fact]
    public async Task Falha_da_Leomob_deixa_a_notificacao_PENDENTE_com_uma_tentativa()
    {
        _leomob.Falhar = true;

        var resultado = await ProcessarAsync(Servico(1012, 21763699));

        var processado = Assert.Single(resultado.Processados);
        Assert.Equal(StatusNotificacao.PENDENTE, processado.Notificacao);
        Assert.Equal(4, processado.LinksNovos);
        var notificacao = Assert.Single(await ObterNotificacoesAsync(1012));
        Assert.Equal((StatusNotificacao.PENDENTE, 1), (notificacao.Status, notificacao.Tentativas));
        Assert.Contains("indisponível", notificacao.UltimoErro);
    }

    [Fact]
    public async Task Envio_repetido_de_pedido_ZMOB_nao_duplica_links_nem_notificacoes()
    {
        await ProcessarAsync(Servico(1012, 21763699));

        var reenvio = await ProcessarAsync(Servico(1012, 21763699));

        Assert.Equal(0, reenvio.Processados.Single().LinksNovos);
        Assert.Null(reenvio.Processados.Single().Notificacao);
        Assert.Equal(4, await ContarLinksAsync(1012));
        Assert.Single(await ObterNotificacoesAsync(1012));
        Assert.Single(_leomob.Chamadas);
    }

    [Fact]
    public async Task Link_novo_com_notificacao_PENDENTE_nao_cria_outra_notificacao()
    {
        _leomob.Falhar = true;
        await ProcessarAsync(Servico(1012, 21763699));
        var reenvio = await ProcessarAsync(ServicoComEtiquetaExtra(1012, 21763699));

        Assert.Equal(1, reenvio.Processados.Single().LinksNovos);
        Assert.Null(reenvio.Processados.Single().Notificacao);
        Assert.Single(await ObterNotificacoesAsync(1012));
        Assert.Single(_leomob.Chamadas);
    }

    [Fact]
    public async Task Link_novo_depois_de_notificacao_CONCLUIDO_gera_nova_notificacao_com_todos_os_links()
    {
        await ProcessarAsync(Servico(1012, 21763699));
        await ProcessarAsync(ServicoComEtiquetaExtra(1012, 21763699));

        Assert.Equal(2, (await ObterNotificacoesAsync(1012)).Count(n => n.Status == StatusNotificacao.CONCLUIDO));
        Assert.Equal(5, _leomob.Chamadas.Last().Count);
    }

    [Fact]
    public async Task Pedidos_ZLPL_e_ZCVS_nao_notificam_a_Leomob()
    {
        await ProcessarAsync(Servico(1007, 21763653), Servico(1008, 21763654));

        Assert.Empty(_leomob.Chamadas);
        Assert.Empty(await ObterNotificacoesAsync(1007));
    }

    [Fact]
    public async Task Envio_simultaneo_do_mesmo_servico_vira_falha_do_item_sem_derrubar_os_demais()
    {
        // Enquanto este envio copia os arquivos (antes de gravar), "outra requisição" grava o mesmo link.
        var outraRequisicaoJaGravou = false;
        _armazenamento.AoCopiar = pedidoSVL =>
        {
            if (pedidoSVL != 1007 || outraRequisicaoJaGravou) return;
            outraRequisicaoJaGravou = true;
            using var outroContexto = _banco.CriarContexto();
            outroContexto.LinksArquivo.Add(new LinkArquivo
            {
                PedidoId = 1,
                Categoria = CategoriaArquivo.CortePdfSumario,
                Url = "https://arquivos.exemplo.com/1007/sumario.pdf",
                DataRecebimento = DateTime.UtcNow
            });
            outroContexto.SaveChanges();
        };

        var resultado = await ProcessarAsync(Servico(1007, 21763653), Servico(1008, 21763654));

        Assert.Contains("ao mesmo tempo", Assert.Single(resultado.Falhas).Motivo);
        Assert.Equal(1008, Assert.Single(resultado.Processados).PedidoSVL);
        Assert.Equal(1, await ContarLinksAsync(1007)); // só o da "outra requisição"
        Assert.Equal(4, await ContarLinksAsync(1008));
    }

    /// <summary>Cada chamada usa um contexto novo, como uma requisição HTTP separada.</summary>
    private async Task<ResultadoProcessamento> ProcessarAsync(params ServicoRequest[] servicos)
    {
        using var contexto = _banco.CriarContexto();
        var envio = new ArquivosProducaoRequest { Services = servicos.ToList() };

        var resultado = await CriarService(contexto).ProcessarAsync(envio, CancellationToken.None);
        return resultado.Resumo!;
    }

    private ArquivosProducaoService CriarService(AppDbContext contexto)
    {
        var notificacoes = new NotificacaoLeomobRepository(contexto);
        var notificacaoLeomob = new NotificacaoLeomobService(_leomob, notificacoes, NullLogger<NotificacaoLeomobService>.Instance);

        return new ArquivosProducaoService(new PedidoRepository(contexto), notificacoes, _armazenamento, notificacaoLeomob,
            NullLogger<ArquivosProducaoService>.Instance);
    }

    private async Task<List<NotificacaoLeomob>> ObterNotificacoesAsync(int pedidoSVL)
    {
        using var contexto = _banco.CriarContexto();
        return await contexto.NotificacoesLeomob.Where(n => n.Pedido.PedidoSVL == pedidoSVL).OrderBy(n => n.Id).ToListAsync();
    }

    private async Task<int> ContarLinksAsync(int pedidoSVL)
    {
        using var contexto = _banco.CriarContexto();
        return await contexto.Pedidos.Where(p => p.PedidoSVL == pedidoSVL).SelectMany(p => p.Links).CountAsync();
    }

    /// <summary>O mesmo serviço de <see cref="Servico"/> com uma etiqueta a mais (etiqueta-2).</summary>
    private static ServicoRequest ServicoComEtiquetaExtra(int pedidoSVL, long serviceId)
    {
        var servico = Servico(pedidoSVL, serviceId);
        servico.Links!.Etiquetas!.Add($"https://arquivos.exemplo.com/{pedidoSVL}/etiqueta-2.png");
        return servico;
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
