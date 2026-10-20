using LeoMadeiras.ApiFoundation.Application.DTOs;
using LeoMadeiras.ApiFoundation.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeoMadeiras.ApiFoundation.Api.Controllers;

[ApiController]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    private readonly ConsultaPedidoService _service;

    public PedidosController(ConsultaPedidoService service) => _service = service;

    /// <summary>Retorna o pedido, seus links e o status da notificação à LeomobAPI (se houver).</summary>
    /// <response code="200">Pedido encontrado.</response>
    /// <response code="400">pedidoSVL não é um número inteiro.</response>
    /// <response code="404">Não existe pedido com esse pedidoSVL.</response>
    [HttpGet("{pedidoSVL}")]
    [ProducesResponseType<PedidoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(int pedidoSVL, CancellationToken cancellationToken)
    {
        var pedido = await _service.ObterAsync(pedidoSVL, cancellationToken);

        return pedido is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Pedido não encontrado",
                detail: $"Não existe pedido com pedidoSVL {pedidoSVL}.")
            : Ok(pedido);
    }
}
