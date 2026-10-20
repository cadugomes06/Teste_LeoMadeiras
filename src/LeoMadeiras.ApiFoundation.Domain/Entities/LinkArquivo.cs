using LeoMadeiras.ApiFoundation.Domain.Enums;

namespace LeoMadeiras.ApiFoundation.Domain.Entities;

public class LinkArquivo
{
    public const int TamanhoMaximoUrl = 2048;

    public long Id { get; set; }
    public int PedidoId { get; set; }
    public CategoriaArquivo Categoria { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime DataRecebimento { get; set; }
}
