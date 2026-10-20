using LeoMadeiras.ApiFoundation.Application.Services;
using Microsoft.Extensions.Options;

namespace LeoMadeiras.ApiFoundation.Api.Workers;

/// <summary>
/// Faz em segundo plano o mesmo que POST /api/notificacoes/reprocessar, a cada intervalo configurado.
/// A regra continua toda no <see cref="NotificacaoLeomobService"/>; aqui só se agenda a execução.
/// </summary>
public class ReprocessamentoNotificacoesWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReprocessamentoOptions _opcoes;
    private readonly ILogger<ReprocessamentoNotificacoesWorker> _logger;

    public ReprocessamentoNotificacoesWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<ReprocessamentoOptions> opcoes,
        ILogger<ReprocessamentoNotificacoesWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _opcoes = opcoes.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Libera a subida da aplicação: com SQLite as chamadas "async" rodam de forma síncrona, e sem isto a
        // primeira rodada seguraria o início da API até terminar.
        await Task.Yield();

        if (!_opcoes.Habilitado)
        {
            _logger.LogInformation("Reprocessamento automático desligado");
            return;
        }

        // Primeira rodada logo ao subir (recupera o que ficou pendente antes de um reinício), depois a cada intervalo.
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_opcoes.IntervaloSegundos));
        do
        {
            try
            {
                // O worker vive a aplicação inteira, mas DbContext e repositórios são por escopo:
                // cada rodada abre o seu, como se fosse uma requisição.
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<NotificacaoLeomobService>().ReprocessarAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Uma rodada que falha (ex.: banco fora do ar) não pode derrubar o worker; tenta na próxima.
                _logger.LogError(ex, "Erro no reprocessamento automático de notificações");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
