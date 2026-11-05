using FcgCatalogApi.Data;
using FcgCatalogApi.Models;
using FcgCatalogApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FcgCatalogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly CatalogDbContext _context;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<GamesController> _logger;

    public GamesController(CatalogDbContext context, IRabbitMqService rabbitMqService, ILogger<GamesController> logger)
    {
        _context = context;
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetGames()
    {
        var games = await _context.Games
            .Where(g => g.Status == "Publicado")
            .ToListAsync();
        return Ok(games);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGame(Guid id)
    {
        var game = await _context.Games.FindAsync(id);
        if (game == null)
        {
            return NotFound();
        }
        return Ok(game);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CreateGame([FromBody] Game game)
    {
        game.Id = Guid.NewGuid();
        game.CriadoEm = DateTime.UtcNow;
        game.Status = "Publicado";

        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> UpdateGame(Guid id, [FromBody] Game game)
    {
        var existingGame = await _context.Games.FindAsync(id);
        if (existingGame == null)
        {
            return NotFound();
        }

        existingGame.Titulo = game.Titulo;
        existingGame.Descricao = game.Descricao;
        existingGame.Genero = game.Genero;
        existingGame.Desenvolvedora = game.Desenvolvedora;
        existingGame.DataLancamento = game.DataLancamento;
        existingGame.Preco = game.Preco;

        await _context.SaveChangesAsync();
        return Ok(existingGame);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DeleteGame(Guid id)
    {
        var game = await _context.Games.FindAsync(id);
        if (game == null)
        {
            return NotFound();
        }

        _context.Games.Remove(game);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/purchase")]
    [Authorize]
    public async Task<IActionResult> PurchaseGame(Guid id)
    {
        var game = await _context.Games.FindAsync(id);
        if (game == null)
        {
            return NotFound(new { message = "Jogo não encontrado" });
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized();
        }

        var userId = Guid.Parse(userIdClaim.Value);

        // Verificar se o usuário já possui o jogo
        var library = await _context.Libraries
            .Include(l => l.Itens)
            .FirstOrDefaultAsync(l => l.UsuarioId == userId);

        if (library != null && library.Itens.Any(i => i.JogoId == id))
        {
            return Conflict(new { message = "Você já possui este jogo" });
        }

        // Publicar evento OrderPlacedEvent
        var orderEvent = new OrderPlacedEvent
        {
            UserId = userId,
            GameId = id,
            Price = game.Preco,
            OrderId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow
        };

        _rabbitMqService.PublishMessage("order.placed", orderEvent);
        _logger.LogInformation($"Pedido criado para usuário {userId} e jogo {id}. OrderId: {orderEvent.OrderId}");

        return Accepted(new { orderId = orderEvent.OrderId, message = "Pedido criado. Processando pagamento..." });
    }

    [HttpGet("library")]
    [Authorize]
    public async Task<IActionResult> GetLibrary()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized();
        }

        var userId = Guid.Parse(userIdClaim.Value);

        var library = await _context.Libraries
            .Include(l => l.Itens)
            .FirstOrDefaultAsync(l => l.UsuarioId == userId);

        if (library == null)
        {
            return Ok(new { items = new List<object>() });
        }

        var gamesIds = library.Itens.Select(i => i.JogoId).ToList();
        var games = await _context.Games
            .Where(g => gamesIds.Contains(g.Id))
            .ToListAsync();

        var result = library.Itens.Select(item => new
        {
            item.JogoId,
            item.ValorPago,
            item.AdquiridoEm,
            Game = games.FirstOrDefault(g => g.Id == item.JogoId)
        }).ToList();

        return Ok(new { items = result });
    }
}
