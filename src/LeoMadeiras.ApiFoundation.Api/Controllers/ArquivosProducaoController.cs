using LeoMadeiras.ApiFoundation.Application.DTOs;
using LeoMadeiras.ApiFoundation.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeoMadeiras.ApiFoundation.Api.Controllers;

[ApiController]
[Route("api/arquivos-producao")]
public class ArquivosProducaoController : ControllerBase
{
    private readonly ArquivosProducaoService _service;

    public ArquivosProducaoController(ArquivosProducaoService service) => _service = service;

    /// <summary>Recebe da Digibee os links dos arquivos de produção de um ou mais serviços.</summary>
    /// <response code="200">Envio processado; a resposta lista os serviços processados e os que falharam.</response>
    /// <response code="400">Corpo fora do contrato (ex.: sem serviceId ou pedidoSVL).</response>
    /// <response code="422">Envio sinalizado com hasErrors = true; nada foi processado.</response>
    [HttpPost]
    [ProducesResponseType<ResultadoProcessamento>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Receber(ArquivosProducaoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _service.ProcessarAsync(request, cancellationToken);

        if (resultado.MotivoRejeicao is not null)
        {
            return Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Envio rejeitado",
                detail: resultado.MotivoRejeicao);
        }

        return Ok(resultado.Resumo);
    }
}
