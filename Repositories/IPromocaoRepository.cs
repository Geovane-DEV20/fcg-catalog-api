using FcgCatalogApi.Models;

namespace FcgCatalogApi.Repositories;

public interface IPromocaoRepository
{
    Task AdicionarAsync(Promocao promocao, CancellationToken cancellationToken = default);
    Task<Promocao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promocao>> ListarVigentesAsync(DateTime agora, CancellationToken cancellationToken = default);
    Task<bool> ExistePromocaoVigenteParaJogoAsync(Guid jogoId, DateTime agora, CancellationToken cancellationToken = default);
}
