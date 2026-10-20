namespace LeoMadeiras.ApiFoundation.Application.Services;

/// <summary>
/// Garante uma rodada de reprocessamento por vez neste processo: o worker e o endpoint manual chamam a mesma
/// rotina, e duas rodadas juntas enviariam a mesma notificação duas vezes. Registrada como singleton.
/// (Com várias instâncias da API seria preciso reservar a notificação no próprio banco.)
/// </summary>
public sealed class TravaReprocessamento
{
    private readonly SemaphoreSlim _semaforo = new(1, 1);

    public Task EsperarAsync(CancellationToken cancellationToken) => _semaforo.WaitAsync(cancellationToken);

    public void Liberar() => _semaforo.Release();
}
