using LeoMadeiras.ApiFoundation.Domain.Enums;

namespace LeoMadeiras.ApiFoundation.Application.DTOs;

/// <summary>
/// Desfecho do envio: ou ele foi rejeitado inteiro (<see cref="MotivoRejeicao"/> preenchido),
/// ou foi processado serviço a serviço (<see cref="Resumo"/> preenchido).
/// </summary>
public record ResultadoEnvio(string? MotivoRejeicao, ResultadoProcessamento? Resumo)
{
    public static ResultadoEnvio Rejeitado(string motivo) => new(motivo, null);
    public static ResultadoEnvio Processado(ResultadoProcessamento resumo) => new(null, resumo);
}

/// <summary>Resumo do envio: o que foi processado e quais serviços falharam (e por quê).</summary>
public record ResultadoProcessamento(IReadOnlyList<ServicoProcessado> Processados, IReadOnlyList<FalhaServico> Falhas);

/// <summary>
/// <see cref="LinksNovos"/> = 0 indica um reenvio sem novidades (nada foi gravado, copiado nem notificado).
/// <see cref="Notificacao"/> é o status da notificação à LeomobAPI criada neste envio (só ZMOB), ou null se nenhuma foi criada.
/// </summary>
public record ServicoProcessado(
    int PedidoSVL, long ServiceId, TipoPedido TipoPedido, int LinksNovos, StatusNotificacao? Notificacao);

public record FalhaServico(int PedidoSVL, long ServiceId, string Motivo);
