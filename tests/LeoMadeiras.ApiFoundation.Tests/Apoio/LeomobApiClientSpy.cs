using LeoMadeiras.ApiFoundation.Application.Interfaces;

namespace LeoMadeiras.ApiFoundation.Tests.Apoio;

/// <summary>LeomobAPI controlável: registra os links de cada chamada e falha só quando o teste pede.</summary>
public class LeomobApiClientSpy : ILeomobApiClient
{
    public List<IReadOnlyCollection<string>> Chamadas { get; } = new();

    public bool Falhar { get; set; }

    /// <summary>Executado no momento da chamada, antes de responder (ex.: conferir o que já está no banco).</summary>
    public Action? AoNotificar { get; set; }

    public Task NotificarArquivosDisponiveisAsync(
        int pedidoSVL, long serviceId, IReadOnlyCollection<string> links, CancellationToken cancellationToken)
    {
        AoNotificar?.Invoke();
        Chamadas.Add(links);

        if (Falhar)
            throw new HttpRequestException("LeomobAPI indisponível (falha simulada).");

        return Task.CompletedTask;
    }
}
