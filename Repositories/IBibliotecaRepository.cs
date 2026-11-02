using FcgCatalogApi.Models;

namespace FcgCatalogApi.Repositories;

public interface IBibliotecaRepository
{
    Task AdicionarAsync(BibliotecaItem item, CancellationToken cancellationToken = default);
    Task<bool> ExisteNaBibliotecaAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default);
    Task<bool> ExistePorOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BibliotecaItem>> ListarPorUsuarioAsync(Guid userId, CancellationToken cancellationToken = default);
}
