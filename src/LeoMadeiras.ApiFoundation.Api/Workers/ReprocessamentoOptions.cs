namespace LeoMadeiras.ApiFoundation.Api.Workers;

public class ReprocessamentoOptions
{
    public const string Secao = "ReprocessamentoAutomatico";

    /// <summary>Desligado se não configurado; o appsettings.json liga.</summary>
    public bool Habilitado { get; set; }

    /// <summary>Intervalo entre duas rodadas de reprocessamento.</summary>
    public int IntervaloSegundos { get; set; } = 30;
}
