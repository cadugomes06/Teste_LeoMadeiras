using LeoMadeiras.ApiFoundation.Application.Interfaces;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LeoMadeiras.ApiFoundation.Infrastructure.Armazenamento;

/// <summary>Não baixa nada: apenas registra no log o arquivo que seria copiado.</summary>
public class ArmazenamentoArquivosFake : IArmazenamentoArquivos
{
    private readonly ILogger<ArmazenamentoArquivosFake> _logger;

    public ArmazenamentoArquivosFake(ILogger<ArmazenamentoArquivosFake> logger) => _logger = logger;

    public Task CopiarAsync(int pedidoSVL, CategoriaArquivo categoria, string url, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        _logger.LogInformation(
            "Pedido {PedidoSVL}: arquivo {Categoria} seria copiado de {Url} para o armazenamento interno",
            pedidoSVL, categoria, url);

        return Task.CompletedTask;
    }
}
