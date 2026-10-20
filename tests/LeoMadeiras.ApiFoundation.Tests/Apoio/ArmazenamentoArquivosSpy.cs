using LeoMadeiras.ApiFoundation.Application.Interfaces;
using LeoMadeiras.ApiFoundation.Domain.Enums;

namespace LeoMadeiras.ApiFoundation.Tests.Apoio;

/// <summary>Guarda cada cópia pedida, para os testes conferirem quantas vezes e de quais arquivos.</summary>
public class ArmazenamentoArquivosSpy : IArmazenamentoArquivos
{
    public List<(int PedidoSVL, CategoriaArquivo Categoria, string Url)> Copias { get; } = new();

    /// <summary>
    /// Executado no início de cada cópia, com o pedidoSVL. Permite simular uma falha (lançando exceção)
    /// ou outra requisição agindo enquanto esta ainda não gravou.
    /// </summary>
    public Action<int>? AoCopiar { get; set; }

    public Task CopiarAsync(int pedidoSVL, CategoriaArquivo categoria, string url, CancellationToken cancellationToken)
    {
        AoCopiar?.Invoke(pedidoSVL);
        Copias.Add((pedidoSVL, categoria, url));
        return Task.CompletedTask;
    }
}
