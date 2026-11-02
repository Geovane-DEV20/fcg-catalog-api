using FcgCatalogApi.DTOs;
using FcgCatalogApi.Repositories;

namespace FcgCatalogApi.Services;

public sealed class BibliotecaService
{
    private readonly IBibliotecaRepository _bibliotecaRepository;
    private readonly IJogoRepository _jogoRepository;
    private readonly ILogger<BibliotecaService> _logger;

    public BibliotecaService(
        IBibliotecaRepository bibliotecaRepository,
        IJogoRepository jogoRepository,
        ILogger<BibliotecaService> logger)
    {
        _bibliotecaRepository = bibliotecaRepository;
        _jogoRepository = jogoRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BibliotecaItemResponse>> ListarBibliotecaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var itens = await _bibliotecaRepository.ListarPorUsuarioAsync(userId, cancellationToken);
        
        var resultado = new List<BibliotecaItemResponse>();
        
        foreach (var item in itens)
        {
            var jogo = await _jogoRepository.ObterPorIdAsync(item.GameId, cancellationToken);
            
            resultado.Add(new BibliotecaItemResponse(
                item.Id,
                item.GameId,
                jogo?.Titulo,
                item.OrderId,
                item.PrecoPago,
                item.AdquiridoEm));
        }

        return resultado;
    }

    public async Task<bool> UsuarioPossuiJogoAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _bibliotecaRepository.ExisteNaBibliotecaAsync(userId, gameId, cancellationToken);
    }
}
