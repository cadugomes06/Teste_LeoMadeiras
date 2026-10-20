using LeoMadeiras.ApiFoundation.Application.Interfaces;
using LeoMadeiras.ApiFoundation.Domain.Enums;

namespace LeoMadeiras.ApiFoundation.Tests.Apoio;

/// <summary>Guarda cada cópia pedida, para os testes conferirem quantas vezes e de quais arquivos.</summary>
public class ArmazenamentoArquivosSpy : IArmazenamentoArquivos
{
    public List<(int PedidoSVL, CategoriaArquivo Categoria, string Url)> Copias { get; } = new();

    /// <summary>Quando preenchido, as cópias deste pedido falham (simula o armazenamento fora do ar).</summary>
    public int? FalharParaPedidoSVL { get; set; }

    public Task CopiarAsync(int pedidoSVL, CategoriaArquivo categoria, string url, CancellationToken cancellationToken)
    {
        if (pedidoSVL == FalharParaPedidoSVL)
            throw new IOException("Armazenamento indisponível (falha simulada).");

        Copias.Add((pedidoSVL, categoria, url));
        return Task.CompletedTask;
    }
}
