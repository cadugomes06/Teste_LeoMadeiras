namespace LeoMadeiras.ApiFoundation.Application.DTOs;

/// <summary>
/// Resumo de uma rodada de reprocessamento: quantas notificações foram tentadas e como cada uma terminou.
/// <see cref="Falhas"/> conta as que esgotaram as tentativas nesta rodada e não serão mais tentadas.
/// </summary>
public record ResultadoReprocessamento(int Reprocessadas, int Concluidas, int Pendentes, int Falhas);
