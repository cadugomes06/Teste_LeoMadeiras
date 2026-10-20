namespace LeoMadeiras.ApiFoundation.Tests.Apoio;

/// <summary>Monta o corpo do POST /api/arquivos-producao no formato que a Digibee envia.</summary>
public static class EnvioArquivos
{
    public const string Rota = "/api/arquivos-producao";

    public static object Envio(bool hasErrors, params object?[] services) =>
        new { documentType = "filesToProduction", hasErrors, services };
}
