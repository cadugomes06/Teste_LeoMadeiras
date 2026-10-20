using LeoMadeiras.ApiFoundation.Application.DTOs;
using LeoMadeiras.ApiFoundation.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeoMadeiras.ApiFoundation.Api.Controllers;

[ApiController]
[Route("api/notificacoes")]
public class NotificacoesController : ControllerBase
{
    private readonly NotificacaoLeomobService _service;

    public NotificacoesController(NotificacaoLeomobService service) => _service = service;

    /// <summary>Simula o worker: tenta de novo as notificações à LeomobAPI que ficaram pendentes.</summary>
    /// <response code="200">Resumo do reprocessamento (quantas concluídas, ainda pendentes e em falha).</response>
    [HttpPost("reprocessar")]
    [ProducesResponseType<ResultadoReprocessamento>(StatusCodes.Status200OK)]
    public Task<ResultadoReprocessamento> Reprocessar(CancellationToken cancellationToken) =>
        _service.ReprocessarAsync(cancellationToken);
}
