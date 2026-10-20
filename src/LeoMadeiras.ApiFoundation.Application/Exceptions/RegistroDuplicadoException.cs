namespace LeoMadeiras.ApiFoundation.Application.Exceptions;

/// <summary>
/// A gravação violou uma regra de unicidade do banco: outro processamento gravou o mesmo registro antes
/// (ex.: a Digibee mandou o mesmo serviço duas vezes ao mesmo tempo).
/// </summary>
public class RegistroDuplicadoException : Exception
{
    public RegistroDuplicadoException(string message, Exception innerException) : base(message, innerException) { }
}
