using FcgCatalogApi.Data;
using FcgCatalogApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FcgCatalogApi.Repositories;

public sealed class JogoRepository : IJogoRepository
{
    private readonly ApplicationDbContext _context;

    public JogoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(Jogo jogo, CancellationToken cancellationToken = default) =>
        await _context.Jogos.AddAsync(jogo, cancellationToken);

    public Task<Jogo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Jogos.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public Task<bool> ExisteComTituloAsync(string titulo, CancellationToken cancellationToken = default) =>
        _context.Jogos.AnyAsync(j => j.Titulo == titulo, cancellationToken);

    public async Task<IReadOnlyList<Jogo>> ListarPublicadosAsync(string? titulo, string? genero, CancellationToken cancellationToken = default)
    {
        var query = _context.Jogos.Where(j => j.Status == StatusJogo.Publicado);

        if (!string.IsNullOrWhiteSpace(titulo))
            query = query.Where(j => j.Titulo.Contains(titulo));

        if (!string.IsNullOrWhiteSpace(genero))
            query = query.Where(j => j.Genero == genero);

        return await query.OrderBy(j => j.Titulo).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Jogo>> ListarTodosAsync(CancellationToken cancellationToken = default) =>
        await _context.Jogos.OrderBy(j => j.Titulo).ToListAsync(cancellationToken);
}
