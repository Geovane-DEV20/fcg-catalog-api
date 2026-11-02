using FcgCatalogApi.DTOs;
using FcgCatalogApi.Middlewares;
using FcgCatalogApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FcgCatalogApi.Controllers;

[ApiController]
[Route("api/biblioteca")]
[Produces("application/json")]
public sealed class BibliotecaController : ControllerBase
{
    private readonly BibliotecaService _bibliotecaService;

    public BibliotecaController(BibliotecaService bibliotecaService)
    {
        _bibliotecaService = bibliotecaService;
    }

    /// <summary>Lista todos os jogos da biblioteca do usuário autenticado.</summary>
    [HttpGet]
    [Authorize(Policy = "UserOrAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<BibliotecaItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListarMinhaBiblioteca(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new ErrorResponse("Não autorizado", "Usuário não identificado no token.", "USUARIO_NAO_IDENTIFICADO", HttpContext.TraceIdentifier));

        var biblioteca = await _bibliotecaService.ListarBibliotecaAsync(userId, cancellationToken);
        return Ok(biblioteca);
    }

    /// <summary>Verifica se o usuário possui um jogo específico na biblioteca.</summary>
    [HttpGet("possui/{gameId:guid}")]
    [Authorize(Policy = "UserOrAdmin")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerificarPossui(Guid gameId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new ErrorResponse("Não autorizado", "Usuário não identificado no token.", "USUARIO_NAO_IDENTIFICADO", HttpContext.TraceIdentifier));

        var possui = await _bibliotecaService.UsuarioPossuiJogoAsync(userId, gameId, cancellationToken);
        return Ok(new { possui });
    }

    /// <summary>Endpoint interno para verificar biblioteca de um usuário específico (comunicação entre microsserviços).</summary>
    [HttpGet("usuario/{userId:guid}")]
    [AllowAnonymous] // Comunicação interna entre serviços
    [ProducesResponseType(typeof(IReadOnlyList<BibliotecaItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarBibliotecaUsuario(Guid userId, CancellationToken cancellationToken)
    {
        var biblioteca = await _bibliotecaService.ListarBibliotecaAsync(userId, cancellationToken);
        return Ok(biblioteca);
    }
}
