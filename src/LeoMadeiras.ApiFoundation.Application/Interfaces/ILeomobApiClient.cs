namespace LeoMadeiras.ApiFoundation.Application.Interfaces;

/// <summary>Cliente da LeomobAPI, que atende os pedidos ZMOB.</summary>
public interface ILeomobApiClient
{
    /// <summary>
    /// Avisa que os arquivos de produção do pedido estão disponíveis, enviando <b>todos</b> os links do pedido
    /// (assim a primeira tentativa e as retentativas do reprocessamento enviam o mesmo conteúdo).
    /// Qualquer exceção significa que a notificação não foi entregue e deve ser tentada de novo.
    /// </summary>
    Task NotificarArquivosDisponiveisAsync(
        int pedidoSVL, long serviceId, IReadOnlyCollection<string> links, CancellationToken cancellationToken);
}
