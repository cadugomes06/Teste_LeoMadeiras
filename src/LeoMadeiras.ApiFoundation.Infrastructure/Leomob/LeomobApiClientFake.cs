using LeoMadeiras.ApiFoundation.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeoMadeiras.ApiFoundation.Infrastructure.Leomob;

/// <summary>Simula a LeomobAPI, falhando aleatoriamente conforme <see cref="LeomobFakeOptions.TaxaFalha"/>.</summary>
public class LeomobApiClientFake : ILeomobApiClient
{
    private readonly LeomobFakeOptions _opcoes;
    private readonly ILogger<LeomobApiClientFake> _logger;

    public LeomobApiClientFake(IOptions<LeomobFakeOptions> opcoes, ILogger<LeomobApiClientFake> logger)
    {
        _opcoes = opcoes.Value;
        _logger = logger;
    }

    public Task NotificarArquivosDisponiveisAsync(
        int pedidoSVL, long serviceId, IReadOnlyCollection<string> links, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        if (Random.Shared.NextDouble() < _opcoes.TaxaFalha)
        {
            _logger.LogWarning("Pedido {PedidoSVL}: falha simulada ao notificar a LeomobAPI", pedidoSVL);
            return Task.FromException(new HttpRequestException("LeomobAPI indisponível (falha simulada)."));
        }

        _logger.LogInformation(
            "Pedido {PedidoSVL}: LeomobAPI notificada (serviceId {ServiceId}, {QuantidadeLinks} links)",
            pedidoSVL, serviceId, links.Count);

        return Task.CompletedTask;
    }
}
