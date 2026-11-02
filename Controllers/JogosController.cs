using FcgCatalogApi.DTOs;
using FcgCatalogApi.Middlewares;
using FcgCatalogApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FcgCatalogApi.Controllers;

[ApiController]
[Route("api/jogos")]
[Produces("application/json")]
public sealed class JogosController : ControllerBase
{
    private readonly JogoService _jogoService;

    public JogosController(JogoService jogoService)
    {
        _jogoService = jogoService;
    }

    /// <summary>Cadastra um novo jogo. Nasce em status Rascunho.</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(CadastrarJogoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cadastrar(CadastrarJogoRequest request, CancellationToken cancellationToken)
    {
        var resposta = await _jogoService.CadastrarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
    }

    /// <summary>Preenche a ficha técnica do jogo (desenvolvedora, data de lançamento).</summary>
    [HttpPut("{id:guid}/ficha")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreencherFicha(Guid id, PreencherFichaRequest request, CancellationToken cancellationToken)
    {
        await _jogoService.PreencherFichaAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Define o preço do jogo.</summary>
    [HttpPut("{id:guid}/preco")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DefinirPreco(Guid id, DefinirPrecoRequest request, CancellationToken cancellationToken)
    {
        await _jogoService.DefinirPrecoAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Atualiza a descrição do jogo.</summary>
    [HttpPut("{id:guid}/descricao")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarDescricao(Guid id, AtualizarDescricaoRequest request, CancellationToken cancellationToken)
    {
        await _jogoService.AtualizarDescricaoAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Publica o jogo no catálogo. Requer ficha preenchida e preço definido.</summary>
    [HttpPost("{id:guid}/publicacao")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Publicar(Guid id, CancellationToken cancellationToken)
    {
        await _jogoService.PublicarAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Remove o jogo do catálogo (despublica).</summary>
    [HttpDelete("{id:guid}/publicacao")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Despublicar(Guid id, CancellationToken cancellationToken)
    {
        await _jogoService.DespublicarAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Lista jogos publicados no catálogo (vitrine). Acesso público.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<JogoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConsultarCatalogo([FromQuery] string? titulo, [FromQuery] string? genero, CancellationToken cancellationToken)
    {
        var resposta = await _jogoService.ConsultarCatalogoAsync(titulo, genero, cancellationToken);
        return Ok(resposta);
    }

    /// <summary>Obtém detalhes de um jogo publicado por ID. Acesso público.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JogoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resposta = await _jogoService.ObterPublicadoPorIdAsync(id, cancellationToken);
        return Ok(resposta);
    }

    /// <summary>Endpoint interno para consulta de jogo para aquisição (comunicação entre microsserviços).</summary>
    [HttpGet("{id:guid}/aquisicao")]
    [AllowAnonymous] // Comunicação interna entre serviços
    [ProducesResponseType(typeof(JogoParaAquisicaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterParaAquisicao(Guid id, CancellationToken cancellationToken)
    {
        var resposta = await _jogoService.ObterParaAquisicaoAsync(id, cancellationToken);
        
        if (resposta is null)
            return NotFound();

        return Ok(resposta);
    }

    /// <summary>Lista todos os jogos (admin). Inclui rascunhos e despublicados.</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<JogoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarTodos(CancellationToken cancellationToken)
    {
        var resposta = await _jogoService.ListarTodosAsync(cancellationToken);
        return Ok(resposta);
    }

    /// <summary>
    /// Inicia uma compra de jogo. Publica OrderPlacedEvent e retorna OrderId para rastreio.
    /// O jogo NÃO é adicionado à biblioteca antes da aprovação do pagamento.
    /// </summary>
    [HttpPost("{id:guid}/comprar")]
    [Authorize(Policy = "UserOrAdmin")]
    [ProducesResponseType(typeof(CompraIniciadaResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Comprar(Guid id, CancellationToken cancellationToken)
    {
        // Obter UserId do token JWT
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new ErrorResponse("Não autorizado", "Usuário não identificado no token.", "USUARIO_NAO_IDENTIFICADO", HttpContext.TraceIdentifier));

        var resposta = await _jogoService.IniciarCompraAsync(id, userId, cancellationToken);
        return Accepted(resposta);
    }
}
