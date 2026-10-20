using LeoMadeiras.ApiFoundation.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LeoMadeiras.ApiFoundation.Tests.Apoio;

/// <summary>
/// SQLite em memória: aplica índices únicos e transações de verdade (o provider InMemory do EF não aplica).
/// O banco vive enquanto a conexão estiver aberta, então cada teste ganha um banco novo e já com o seed.
/// </summary>
public sealed class BancoEmMemoria : IDisposable
{
    private readonly SqliteConnection _conexao;
    private readonly DbContextOptions<AppDbContext> _opcoes;

    public BancoEmMemoria()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();
        _opcoes = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options;

        using var contexto = CriarContexto();
        contexto.Database.EnsureCreated();
    }

    /// <summary>Conexão que mantém o banco vivo; a API dos testes de integração usa a mesma.</summary>
    public SqliteConnection Conexao => _conexao;

    public AppDbContext CriarContexto() => new(_opcoes);

    public void Dispose() => _conexao.Dispose();
}
