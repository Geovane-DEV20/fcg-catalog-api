using FcgCatalogApi.DTOs;
using FcgCatalogApi.Middlewares;
using FcgCatalogApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FcgCatalogApi.Controllers;

[ApiController]
[Route("api/promocoes")]
[Produces("application/json")]
public sealed class PromocoesController : ControllerBase
{
    private readonly PromocaoService _promocaoService;

    public PromocoesController(PromocaoService promocaoService)
    {
        _promocaoService = promocaoService;
    }

    /// <summary>Cria uma nova promoção.</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(CriarPromocaoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Criar(CriarPromocaoRequest request, CancellationToken cancellationToken)
    {
        var resposta = await _promocaoService.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ListarVigentes), null, resposta);
    }

    /// <summary>Aplica a promoção a jogos publicados.</summary>
    [HttpPost("{id:guid}/jogos")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AplicarAJogos(Guid id, AplicarPromocaoAJogosRequest request, CancellationToken cancellationToken)
    {
        await _promocaoService.AplicarAJogosAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Encerra uma promoção manualmente.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Encerrar(Guid id, CancellationToken cancellationToken)
    {
        await _promocaoService.EncerrarAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Lista promoções vigentes. Acesso público.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PromocaoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarVigentes(CancellationToken cancellationToken)
    {
        var resposta = await _promocaoService.ListarVigentesAsync(cancellationToken);
        return Ok(resposta);
    }
}
