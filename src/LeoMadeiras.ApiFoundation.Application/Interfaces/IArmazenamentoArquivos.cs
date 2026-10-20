using LeoMadeiras.ApiFoundation.Domain.Enums;

namespace LeoMadeiras.ApiFoundation.Application.Interfaces;

/// <summary>Copia um arquivo de produção (gerado pelo CorteCloud) para o armazenamento interno da Leo.</summary>
public interface IArmazenamentoArquivos
{
    Task CopiarAsync(int pedidoSVL, CategoriaArquivo categoria, string url, CancellationToken cancellationToken);
}
