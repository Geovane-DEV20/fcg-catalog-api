using FcgCatalogApi.Data;
using FcgCatalogApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FcgCatalogApi.Repositories;

public sealed class BibliotecaRepository : IBibliotecaRepository
{
    private readonly ApplicationDbContext _context;

    public BibliotecaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(BibliotecaItem item, CancellationToken cancellationToken = default) =>
        await _context.BibliotecaItens.AddAsync(item, cancellationToken);

    public Task<bool> ExisteNaBibliotecaAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default) =>
        _context.BibliotecaItens.AnyAsync(b => b.UserId == userId && b.GameId == gameId, cancellationToken);

    public Task<bool> ExistePorOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        _context.BibliotecaItens.AnyAsync(b => b.OrderId == orderId, cancellationToken);

    public async Task<IReadOnlyList<BibliotecaItem>> ListarPorUsuarioAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.BibliotecaItens
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.AdquiridoEm)
            .ToListAsync(cancellationToken);
}
