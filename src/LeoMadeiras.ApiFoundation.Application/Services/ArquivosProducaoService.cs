using LeoMadeiras.ApiFoundation.Application.DTOs;
using LeoMadeiras.ApiFoundation.Application.Interfaces;
using LeoMadeiras.ApiFoundation.Domain.Entities;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LeoMadeiras.ApiFoundation.Application.Services;

/// <summary>Processa os links dos arquivos de produção recebidos da Digibee.</summary>
public class ArquivosProducaoService
{
    private readonly IPedidoRepository _pedidos;
    private readonly IArmazenamentoArquivos _armazenamento;
    private readonly ILogger<ArquivosProducaoService> _logger;

    public ArquivosProducaoService(
        IPedidoRepository pedidos, IArmazenamentoArquivos armazenamento, ILogger<ArquivosProducaoService> logger)
    {
        _pedidos = pedidos;
        _armazenamento = armazenamento;
        _logger = logger;
    }

    public async Task<ResultadoEnvio> ProcessarAsync(ArquivosProducaoRequest envio, CancellationToken cancellationToken)
    {
        // Regra 1: o CorteCloud sinalizou erro na geração dos arquivos, então o envio inteiro é rejeitado.
        if (envio.HasErrors)
        {
            _logger.LogWarning("Envio rejeitado: hasErrors = true ({QuantidadeServicos} serviços)", envio.Services.Count);
            return ResultadoEnvio.Rejeitado(
                "O envio chegou com hasErrors = true: os arquivos de produção não foram gerados " +
                "corretamente e nenhum serviço foi processado.");
        }

        var processados = new List<ServicoProcessado>();
        var falhas = new List<FalhaServico>();

        // Cada serviço é tratado de forma independente: a falha de um não impede os demais (regra 2).
        foreach (var servico in envio.Services)
        {
            try
            {
                var pedido = await _pedidos.ObterComLinksAsync(servico.PedidoSVL, cancellationToken);
                if (pedido is null)
                {
                    falhas.Add(Falha(servico, $"Pedido {servico.PedidoSVL} não encontrado."));
                    continue;
                }

                if (pedido.ServiceId != servico.ServiceId)
                {
                    falhas.Add(Falha(servico,
                        $"serviceId {servico.ServiceId} não corresponde ao serviceId do pedido {pedido.PedidoSVL}."));
                    continue;
                }

                var linksNovos = await ProcessarLinksNovosAsync(pedido, servico.Links, cancellationToken);

                _logger.LogInformation("Pedido {PedidoSVL}: {QuantidadeLinks} links novos recebidos", pedido.PedidoSVL, linksNovos);
                processados.Add(new ServicoProcessado(pedido.PedidoSVL, pedido.ServiceId, pedido.TipoPedido, linksNovos));
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // O que não foi gravado deste serviço não pode ir junto no SaveChanges do próximo.
                _pedidos.DescartarAlteracoes();
                _logger.LogError(ex, "Pedido {PedidoSVL}: erro inesperado ao processar o serviço {ServiceId}",
                    servico.PedidoSVL, servico.ServiceId);
                falhas.Add(new FalhaServico(servico.PedidoSVL, servico.ServiceId,
                    "Erro inesperado ao processar o serviço. Reenvie para tentar de novo."));
            }
        }

        return ResultadoEnvio.Processado(new ResultadoProcessamento(processados, falhas));
    }

    /// <summary>
    /// Trata apenas os links que o pedido ainda não tem, então um reenvio idêntico não gera nenhum efeito (regra 7).
    /// Retorna quantos links novos foram gravados.
    /// </summary>
    private async Task<int> ProcessarLinksNovosAsync(Pedido pedido, LinksRequest? links, CancellationToken cancellationToken)
    {
        var existentes = pedido.Links.Select(l => (l.Categoria, l.Url)).ToHashSet();
        var linksNovos = (links?.Achatar() ?? []).Where(l => !existentes.Contains(l)).ToList();
        if (linksNovos.Count == 0)
            return 0;

        // Regra 4: ZLPL e ZCVS copiam os arquivos; regra 5: ZMOB não baixa nada.
        // A cópia vem antes de gravar: se falhar, nada é gravado e o reenvio da Digibee tenta tudo de novo.
        // (Gravar antes faria um reenvio achar que o link já foi tratado e o arquivo nunca seria copiado.)
        if (pedido.TipoPedido is TipoPedido.ZLPL or TipoPedido.ZCVS)
        {
            foreach (var (categoria, url) in linksNovos)
                await _armazenamento.CopiarAsync(pedido.PedidoSVL, categoria, url, cancellationToken);
        }

        var agora = DateTime.UtcNow;
        pedido.Links.AddRange(linksNovos.Select(l => new LinkArquivo
        {
            PedidoId = pedido.Id,
            Categoria = l.Categoria,
            Url = l.Url,
            DataRecebimento = agora
        }));
        await _pedidos.SalvarAlteracoesAsync(cancellationToken);

        return linksNovos.Count;
    }

    private FalhaServico Falha(ServicoRequest servico, string motivo)
    {
        _logger.LogWarning("Pedido {PedidoSVL}: serviço {ServiceId} não processado. {Motivo}",
            servico.PedidoSVL, servico.ServiceId, motivo);
        return new FalhaServico(servico.PedidoSVL, servico.ServiceId, motivo);
    }
}
