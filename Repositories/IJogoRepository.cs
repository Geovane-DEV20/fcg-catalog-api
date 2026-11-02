using FcgCatalogApi.Models;

namespace FcgCatalogApi.Repositories;

public interface IJogoRepository
{
    Task AdicionarAsync(Jogo jogo, CancellationToken cancellationToken = default);
    Task<Jogo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExisteComTituloAsync(string titulo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Jogo>> ListarPublicadosAsync(string? titulo, string? genero, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Jogo>> ListarTodosAsync(CancellationToken cancellationToken = default);
}
