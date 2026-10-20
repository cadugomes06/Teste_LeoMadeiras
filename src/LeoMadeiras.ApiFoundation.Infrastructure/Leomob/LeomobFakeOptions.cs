namespace LeoMadeiras.ApiFoundation.Infrastructure.Leomob;

public class LeomobFakeOptions
{
    public const string Secao = "LeomobFake";

    /// <summary>Probabilidade de falha de cada chamada: 0 nunca falha, 1 sempre falha.</summary>
    public double TaxaFalha { get; set; }
}
