using FcgCatalogApi.Models;

namespace FcgCatalogApi.DTOs;

// Requests
public sealed record CriarPromocaoRequest(string Nome, decimal Percentual, DateTime Inicio, DateTime Fim);

public sealed record AplicarPromocaoAJogosRequest(IReadOnlyList<Guid> JogoIds);

// Responses
public sealed record CriarPromocaoResponse(Guid Id);

public sealed record PromocaoResponse(
    Guid Id, 
    string Nome, 
    decimal Percentual, 
    DateTime Inicio, 
    DateTime Fim, 
    IReadOnlyCollection<Guid> JogoIds)
{
    public static PromocaoResponse DePromocao(Promocao promocao) => new(
        promocao.Id,
        promocao.Nome,
        promocao.PercentualDesconto,
        promocao.VigenciaInicio,
        promocao.VigenciaFim,
        promocao.JogoIds);
}
