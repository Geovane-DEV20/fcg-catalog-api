using FcgCatalogApi.Data;
using FcgCatalogApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FcgCatalogApi.Repositories;

public sealed class PromocaoRepository : IPromocaoRepository
{
    private readonly ApplicationDbContext _context;

    public PromocaoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(Promocao promocao, CancellationToken cancellationToken = default) =>
        await _context.Promocoes.AddAsync(promocao, cancellationToken);

    public Task<Promocao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Promocoes.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Promocao>> ListarVigentesAsync(DateTime agora, CancellationToken cancellationToken = default)
    {
        var promocoes = await _context.Promocoes
            .Where(p => !p.EncerradaManualmente)
            .ToListAsync(cancellationToken);

        return promocoes.Where(p => p.EstaVigente(agora)).ToList();
    }

    public async Task<bool> ExistePromocaoVigenteParaJogoAsync(Guid jogoId, DateTime agora, CancellationToken cancellationToken = default)
    {
        var vigentes = await ListarVigentesAsync(agora, cancellationToken);
        return vigentes.Any(p => p.JogoIds.Contains(jogoId));
    }
}
