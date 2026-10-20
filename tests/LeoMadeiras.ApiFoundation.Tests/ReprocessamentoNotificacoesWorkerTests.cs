using LeoMadeiras.ApiFoundation.Domain.Entities;
using LeoMadeiras.ApiFoundation.Domain.Enums;
using LeoMadeiras.ApiFoundation.Infrastructure.Persistence;
using LeoMadeiras.ApiFoundation.Tests.Apoio;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LeoMadeiras.ApiFoundation.Tests;

public sealed class ReprocessamentoNotificacoesWorkerTests : IDisposable
{
    // O worker roda em outra thread ao mesmo tempo que o teste consulta o banco. Uma SqliteConnection não pode
    // ser usada por duas threads, então aqui o banco é um arquivo temporário e cada contexto abre a sua conexão.
    private readonly string _arquivoBanco = Path.Combine(Path.GetTempPath(), $"apifoundation-worker-{Guid.NewGuid():N}.db");
    private string ConnectionString => $"Data Source={_arquivoBanco}";

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_arquivoBanco);
    }

    [Fact]
    public async Task Worker_ligado_reprocessa_as_notificacoes_pendentes_sozinho()
    {
        using var api = new ApiFactory(connectionString: ConnectionString, reprocessamentoAutomatico: true);
        api.CreateClient(); // sobe a aplicação: cria o banco (com o seed) e liga o worker

        using (var contexto = CriarContexto())
        {
            contexto.NotificacoesLeomob.Add(new NotificacaoLeomob
            {
                PedidoId = 3, // pedido ZMOB do seed (1012)
                Status = StatusNotificacao.PENDENTE,
                Tentativas = 1,
                DataCriacao = DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow
            });
            await contexto.SaveChangesAsync();
        }

        var status = await AguardarStatusAsync(StatusNotificacao.CONCLUIDO, TimeSpan.FromSeconds(10));

        Assert.Equal(StatusNotificacao.CONCLUIDO, status);
    }

    private AppDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(ConnectionString).Options);

    /// <summary>Consulta o banco até a notificação chegar ao status esperado ou o tempo acabar.</summary>
    private async Task<StatusNotificacao> AguardarStatusAsync(StatusNotificacao esperado, TimeSpan limite)
    {
        var fim = DateTime.UtcNow + limite;
        StatusNotificacao atual;
        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            using var contexto = CriarContexto();
            atual = (await contexto.NotificacoesLeomob.SingleAsync()).Status;
        } while (atual != esperado && DateTime.UtcNow < fim);

        return atual;
    }
}
