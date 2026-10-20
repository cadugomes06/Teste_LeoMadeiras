using System.ComponentModel.DataAnnotations;
using LeoMadeiras.ApiFoundation.Domain.Entities;
using LeoMadeiras.ApiFoundation.Domain.Enums;

namespace LeoMadeiras.ApiFoundation.Application.DTOs;

/// <summary>Envio da Digibee com os links dos arquivos de produção gerados pelo CorteCloud.</summary>
public class ArquivosProducaoRequest : IValidatableObject
{
    public string? DocumentType { get; set; }
    public bool HasErrors { get; set; }

    [MinLength(1, ErrorMessage = "Informe ao menos um serviço.")]
    public List<ServicoRequest> Services { get; set; } = new();

    // A validação automática não olha os itens nulos de uma lista ("services": [null]).
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Services.Any(s => s is null))
            yield return new ValidationResult("A lista de serviços não pode ter itens vazios.", new[] { nameof(Services) });
    }
}

public class ServicoRequest
{
    // Ids são sempre positivos: campo ausente chega como 0 e é recusado aqui.
    [Range(1, long.MaxValue, ErrorMessage = "serviceId é obrigatório e deve ser positivo.")]
    public long ServiceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "pedidoSVL é obrigatório e deve ser positivo.")]
    public int PedidoSVL { get; set; }

    public LinksRequest? Links { get; set; }
}

public class LinksRequest : IValidatableObject
{
    public LinksCorteRequest? Corte { get; set; }
    public List<string>? Etiquetas { get; set; }
    public List<string>? Usinagem { get; set; }

    /// <summary>
    /// Transforma a estrutura aninhada do CorteCloud em pares (categoria, url), ignorando urls vazias
    /// e repetidas dentro do mesmo envio.
    /// </summary>
    public IReadOnlyList<(CategoriaArquivo Categoria, string Url)> Achatar()
    {
        var porCategoria = new (CategoriaArquivo Categoria, List<string>? Urls)[]
        {
            (CategoriaArquivo.CortePdfSumario, Corte?.PdfSumario),
            (CategoriaArquivo.CortePdfDetalhado, Corte?.PdfDetalhado),
            (CategoriaArquivo.Etiqueta, Etiquetas),
            (CategoriaArquivo.Usinagem, Usinagem)
        };

        return porCategoria
            .SelectMany(c => (c.Urls ?? [])
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => (c.Categoria, Url: url.Trim())))
            .Distinct()
            .ToList();
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (_, url) in Achatar())
        {
            var valida = url.Length <= LinkArquivo.TamanhoMaximoUrl
                         && Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            if (!valida)
                yield return new ValidationResult(
                    $"Link inválido: '{url}'. Informe uma URL http(s) com até {LinkArquivo.TamanhoMaximoUrl} caracteres.");
        }
    }
}

public class LinksCorteRequest
{
    public List<string>? PdfSumario { get; set; }
    public List<string>? PdfDetalhado { get; set; }
}
